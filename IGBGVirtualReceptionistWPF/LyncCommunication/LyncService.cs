using System.Collections;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Lync.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IGBGVirtualReceptionist.LyncCommunication
{
    public class LyncService : IDisposable
    {
        private const int MaxContactSearchResults = 20;

        private LyncClient client;
        private bool thisInitializedLync = false;
        private bool expertSearchEnabled = false;
        private IList<SearchProviders> activeSearchProviders;
        private ContactSubscription searchResultSubscription;

        public event EventHandler<ClientStateChangedEventArgs> ClientStateChanged;
        public event EventHandler ClientDisconnected;
        public event EventHandler<SearchContactsEventArgs> SearchContactsFinished;

        public LyncService()
        {
            this.Initialize();
        }

        public void Initialize()
        {
            try
            {
                this.client = LyncClient.GetClient();

                client.StateChanged += this.Client_StateChanged;
                client.ClientDisconnected += this.Client_ClientDisconnected;

                //if this client is in UISuppressionMode and not yet initialized
                if (client.InSuppressedMode && client.State == ClientState.Uninitialized)
                {
                    // initialize the client
                    try
                    {
                        client.BeginInitialize(this.ClientInitialized, null);
                    }
                    catch (LyncClientException lyncClientException)
                    {
                        Console.WriteLine("LyncService: " + lyncClientException.Message);
                    }
                    catch (SystemException systemException)
                    {
                        if (LyncModelExceptionHelper.IsLyncException(systemException))
                        {
                            Console.WriteLine("LyncService: " + systemException.Message);
                        }
                        else
                        {
                            // Rethrow the SystemException which did not come from the Lync Model API.
                            throw;
                        }
                    }
                }
                else //not in UI Suppression, so the client was already initialized
                {
                    if (this.client.InSuppressedMode == true && this.client.State != ClientState.SignedIn)
                    {
                        this.SingIn();
                    }

                    //registers for conversation related events
                    //these events will occur when new conversations are created (incoming/outgoing) and removed
                   
                    // TODO:
                    //client.ConversationManager.ConversationAdded += ConversationManager_ConversationAdded;
                    //client.ConversationManager.ConversationRemoved += ConversationManager_ConversationRemoved;
                }

            }
            catch (Exception ex)
            {
                //if the Lync process is not running and UISuppressionMode=false these exception will be thrown
                if (ex is ClientNotFoundException || ex is NotStartedByUserException)
                {
                    Console.WriteLine("LyncService: " + ex.Message);
                    MessageBox.Show("Microsoft Lync does not appear to be running. Please start Lync.");

                    return;
                }

                throw;
            }
        }

        public async void Dispose()
        {
            this.client.StateChanged -= this.Client_StateChanged;
            this.client.ClientDisconnected -= this.Client_ClientDisconnected;
            this.client.ContactManager.SearchProviderStateChanged -= this.ContactManagerSearchProviderStateChanged;

            // TODO: 
            //this.client.ConversationManager.ConversationAdded -= this.ConversationManager_ConversationAdded;
            //this.client.ConversationManager.ConversationRemoved -= this.ConversationManager_ConversationRemoved;

            if (this.client.InSuppressedMode && this.thisInitializedLync)
            {
                if (this.client.State == ClientState.SignedIn)
                {
                    await this.SignOut();
                }
                
                if (this.client.State == ClientState.SignedOut)
                {
                    await this.ShutdownClient();
                }
            }
        }

        public IEnumerable<ContactInfo> GetFavoriteContacts()
        {
            var managerContact = this.client.ContactManager.GetContactByUri("sip:zchavdarova@infragistics.com");
            var managerInfo = ContactInfo.GetContactInfo(managerContact);
            yield return managerInfo;

            var hrContact = this.client.ContactManager.GetContactByUri("sip:ptsvetanova@infragistics.com");
            var hrInfo = ContactInfo.GetContactInfo(hrContact);
            yield return hrInfo;

            var hrCoordinator = this.client.ContactManager.GetContactByUri("sip:mstefanova@infragistics.com");
            var hrCoordinatorInfo = ContactInfo.GetContactInfo(hrCoordinator);
            yield return hrCoordinatorInfo;
        }

        public void StartSearchForContactsOrGroups(string searchCriteria)
        {
            if (string.IsNullOrEmpty(searchCriteria) || this.client.State != ClientState.SignedIn)
            {
                return;
            }

            try
            {
                // Initiate search for entity based on name.
                var searchFields = this.client.ContactManager.GetSearchFields();
                object[] asyncState = { this.client.ContactManager, searchCriteria };

                if (expertSearchEnabled)
                {
                    // Get the Sharepoint expert search URL with the user's search string query parameter.
                    var sharePointSearchQueryString = this.client.ContactManager.GetExpertSearchQueryString(searchCriteria);

                    this.client.ContactManager.BeginSearch(sharePointSearchQueryString, SearchProviders.Expert, searchFields,
                        SearchOptions.Default, MaxContactSearchResults, this.SearchResultsCallback, asyncState);
                }
                else
                {
                    this.client.ContactManager.BeginSearch(searchCriteria, this.SearchResultsCallback, asyncState);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("LyncService Error: " + ex.Message);
            }
        }

        private void ClientInitialized(IAsyncResult result)
        {
            if (!result.IsCompleted)
            {
                Console.WriteLine("LyncService: Error initializing the LyncClient");
                return;
            }

            this.thisInitializedLync = true;
            this.client.EndInitialize(result);

            this.activeSearchProviders = new List<SearchProviders>();

            // Loads Expert search provider if it is configured
            SearchProviderStatusType expertSearchProviderStatus = this.client.ContactManager.GetSearchProviderStatus(SearchProviders.Expert);
            if (expertSearchProviderStatus == SearchProviderStatusType.SyncSucceeded ||
                expertSearchProviderStatus == SearchProviderStatusType.SyncSucceededForExternalOnly ||
                expertSearchProviderStatus == SearchProviderStatusType.SyncSucceededForInternalOnly)
            {
                this.activeSearchProviders.Add(SearchProviders.Expert);
                this.expertSearchEnabled = true;
            }

            // Register for the SearchProviderStatusChanged event raised by ContactManager
            this.client.ContactManager.SearchProviderStateChanged += this.ContactManagerSearchProviderStateChanged;


            //registers for conversation related events
            //these events will occur when new conversations are created (incoming/outgoing) and removed
            // TODO:
            //client.ConversationManager.ConversationAdded += ConversationManager_ConversationAdded;
            //client.ConversationManager.ConversationRemoved += ConversationManager_ConversationRemoved;
        }

        private Task SingIn()
        {
            var tcs = new TaskCompletionSource<bool>();

            this.client.CredentialRequested += this.Client_CredentialRequested;

            this.client.BeginSignIn(null, null, null, (ar) =>
            {
                this.client.EndSignIn(ar);

                tcs.SetResult(true);
            }, null);

            return tcs.Task;
        }

        private Task SignOut()
        {
            var tcs = new TaskCompletionSource<bool>();

            this.client.BeginSignOut((ar) =>
            {
                this.client.EndSignOut(ar);

                tcs.SetResult(true);
            }, null);

            return tcs.Task;
        }

        private void Client_StateChanged(object sender, ClientStateChangedEventArgs e)
        {
            var handler = this.ClientStateChanged;
            if (handler != null)
            {
                handler(this, e);
            }

            if (e.NewState == ClientState.SignedOut && this.client.InSuppressedMode == true)
            {
                this.SingIn();
            }
        }

        private void Client_ClientDisconnected(object sender, EventArgs e)
        {
            var handler = this.ClientDisconnected;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void RaiseSearchContactsFinished(object sender, SearchContactsEventArgs e)
        {
            var handler = this.SearchContactsFinished;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void Client_CredentialRequested(object sender, CredentialRequestedEventArgs e)
        {
            //If the server type is Lync server and sign in credentials
            //are needed.
            if (e.Type == CredentialRequestedType.SignIn)
            {
                //Re-submit sign in credentials
                e.Submit("oracle@infragistics.com", "<provide password>", true);
            }
        }

        private Task ShutdownClient()
        {
            var tcs = new TaskCompletionSource<bool>();

            this.client.BeginShutdown((ar) =>
            {
                this.client.EndShutdown(ar);
                tcs.SetResult(true);
            }, null);

            return tcs.Task;
        }

        private void ContactManagerSearchProviderStateChanged(object sender, SearchProviderStateChangedEventArgs e)
        {
            if (e.NewStatus != SearchProviderStatusType.SyncSucceeded)
            {
                // Remove the SearchProviders enumerator to the local application cache declared previously
                activeSearchProviders.Remove(e.Provider);
            }

            // TODO: check which providers we will need. Do we need an external provider?
            if (e.Provider == SearchProviders.Expert)
            {
                this.expertSearchEnabled = e.NewStatus == SearchProviderStatusType.SyncSucceeded ||
                                           e.NewStatus == SearchProviderStatusType.SyncSucceededForExternalOnly ||
                                           e.NewStatus == SearchProviderStatusType.SyncSucceededForInternalOnly;

                if (this.expertSearchEnabled)
                {
                    if (!this.activeSearchProviders.Contains(SearchProviders.Expert))
                        this.activeSearchProviders.Add(SearchProviders.Expert);
                }
                else
                {
                    if (this.activeSearchProviders.Contains(SearchProviders.Expert))
                        this.activeSearchProviders.Remove(SearchProviders.Expert);
                }
            }
        }

        private void SearchResultsCallback(IAsyncResult ar)
        {
            // Check the state of search operation.
            if (ar.IsCompleted == true)
            {
                object[] asyncState = (object[])ar.AsyncState;
                try
                {
                    var results = ((ContactManager)asyncState[0]).EndSearch(ar);
                    if (results.AllResults.Count != 0)
                    {
                        // Subscribe to the search results.
                        SubscribeToSearchResults(results.Contacts);

                        var contactDetails = results.Contacts.Select(x => ContactInfo.GetContactInfo(x));
                        this.RaiseSearchContactsFinished(this, new SearchContactsEventArgs(contactDetails));
                    }
                }
                catch (SearchException se)
                {
                    Console.WriteLine("LyncService Error: " + se.Message);
                }
            }
        }

        public void SubscribeToSearchResults(IEnumerable<Contact> contactsFound)
        {
            try
            {
                if (this.searchResultSubscription == null)
                {
                    // Create subscription for the contact manager
                    // if the contact manager is not subscribed.
                    this.searchResultSubscription = this.client.ContactManager.CreateSubscription();
                }
                else
                {
                    // Remove all existing search results.
                    this.searchResultSubscription.Unsubscribe();
                    foreach (Contact c in searchResultSubscription.Contacts)
                    {
                        this.searchResultSubscription.RemoveContact(c);
                    }
                }

                // Add the Contact to a ContactSubscription.
                this.searchResultSubscription.AddContacts(contactsFound);

                // Specify the Contact Information Types to be
                // returned in ContactInformationChanged events.
                ContactInformationType[] contactInformationTypes = { ContactInformationType.Availability, ContactInformationType.ActivityId };

                // Activate the subscription.
                this.searchResultSubscription.Subscribe(ContactSubscriptionRefreshRate.High, contactInformationTypes);
            }
            catch (Exception ex)
            {
                MessageBox.Show("LyncService Error:    " + ex.Message);
            }
        }
    }
}
