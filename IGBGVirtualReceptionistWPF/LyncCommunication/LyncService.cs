using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;

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
        private Dictionary<Conversation, ConversationType> activeConversationsToConversationTypeMap;

        private List<Contact> favoriteContacts;
        private List<ContactInfo> favoriteContactInfos;

        public event EventHandler<ClientStateChangedEventArgs> ClientStateChanged;
        public event EventHandler ClientDisconnected;
        public event EventHandler<SearchContactsEventArgs> SearchContactsFinished;
        public event EventHandler<ConversationEventArgs> ConversationStarted;
        public event EventHandler<ConversationEventArgs> ConversationEnded;

        public bool InUISuppressedMode
        {
            get
            {
                return this.client.InSuppressedMode;
            }
        }

        public LyncClient Client
        {
            get
            {
                return this.client;
            }
        }

        public LyncService()
        {
            activeConversationsToConversationTypeMap = new Dictionary<Conversation, ConversationType>();

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
                    client.ConversationManager.ConversationAdded += this.ConversationManagerConversationAdded;
                    client.ConversationManager.ConversationRemoved += this.ConversationManagerConversationRemoved;
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

            this.client.ConversationManager.ConversationAdded -= this.ConversationManagerConversationAdded;
            this.client.ConversationManager.ConversationRemoved -= this.ConversationManagerConversationRemoved;

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
            if (this.favoriteContacts == null || this.favoriteContactInfos == null)
                this.InitializeFavoriteContacts();

            return this.favoriteContactInfos;
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

        public bool StartConversation(string contactUri, ConversationType conversationType)
        {
            var contact = this.searchResultSubscription.Contacts.FirstOrDefault(x => x.Uri == contactUri);
            if (contact == null)
            {
                return false;
            }

            //creates a new conversation
            Conversation conversation = null;
            try
            {
                conversation = client.ConversationManager.AddConversation();
                this.activeConversationsToConversationTypeMap.Add(conversation, conversationType);
            }
            catch (LyncClientException e)
            {
                Console.WriteLine("LyncSevice Error: " + e.Message);
            }
            catch (SystemException systemException)
            {
                if (LyncModelExceptionHelper.IsLyncException(systemException))
                {
                    // Log the exception thrown by the Lync Model API.
                    Console.WriteLine("LyncSevice Error: " + systemException);
                }
                else
                {
                    // Rethrow the SystemException which did not come from the Lync Model API.
                    throw;
                }
            }

            //Adds a participant to the conversation
            //the window created for this conversation will handle the ParticipantAdded events
            if (conversation != null)
            {
                try
                {
                    conversation.AddParticipant(contact);
                }
                catch (LyncClientException lyncClientException)
                {
                    Console.WriteLine("LyncSevice Error: " + lyncClientException);
                }
                catch (SystemException systemException)
                {
                    if (LyncModelExceptionHelper.IsLyncException(systemException))
                    {
                        // Log the exception thrown by the Lync Model API.
                        Console.WriteLine("LyncSevice Error: " + systemException);
                    }
                    else
                    {
                        // Rethrow the SystemException which did not come from the Lync Model API.
                        throw;
                    }
                }
            }

            return true;
        }

        private void InitializeFavoriteContacts()
        {
            if (this.favoriteContacts == null)
                this.favoriteContacts = new List<Contact>();

            if (this.favoriteContactInfos == null)
                this.favoriteContactInfos = new List<ContactInfo>();

            if (this.favoriteContactInfos.Count == 0)
            {
                var managerContact = this.client.ContactManager.GetContactByUri("sip:zchavdarova@infragistics.com");
                this.favoriteContacts.Add(managerContact);

                var managerInfo = ContactInfo.GetContactInfo(managerContact);
                this.favoriteContactInfos.Add(managerInfo);

                var hrContact = this.client.ContactManager.GetContactByUri("sip:ptsvetanova@infragistics.com");
                this.favoriteContacts.Add(hrContact);

                var hrInfo = ContactInfo.GetContactInfo(hrContact);
                this.favoriteContactInfos.Add(hrInfo);

                var hrCoordinator = this.client.ContactManager.GetContactByUri("sip:mstefanova@infragistics.com");
                this.favoriteContacts.Add(hrCoordinator);

                var hrCoordinatorInfo = ContactInfo.GetContactInfo(hrCoordinator);
                this.favoriteContactInfos.Add(hrCoordinatorInfo);

                if (this.searchResultSubscription == null)
                {
                    this.searchResultSubscription = this.client.ContactManager.CreateSubscription();
                    this.searchResultSubscription.AddContacts(this.favoriteContacts);
                }
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
            client.ConversationManager.ConversationAdded += this.ConversationManagerConversationAdded;
            client.ConversationManager.ConversationRemoved += this.ConversationManagerConversationRemoved;
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
            if (!this.client.InSuppressedMode || e.NewState == ClientState.SignedIn)
            {
                this.InitializeFavoriteContacts();
            }

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
                e.Submit("<provide user>", "<provide password>", true);
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

        private void SubscribeToSearchResults(IEnumerable<Contact> contactsFound)
        {
            try
            {
                if (this.searchResultSubscription == null)
                {
                    // Create subscription for the contact manager if the contact manager is not subscribed.
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

                this.searchResultSubscription.AddContacts(contactsFound);
                this.searchResultSubscription.AddContacts(this.favoriteContacts);

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

        private void ConversationManagerConversationRemoved(object sender, ConversationManagerEventArgs e)
        {
            // Remove the conversation from active conversations map
            var conversationType = this.activeConversationsToConversationTypeMap[e.Conversation];
            this.activeConversationsToConversationTypeMap.Remove(e.Conversation);

            var handler = this.ConversationEnded;
            if (handler != null)
            {
                var contactInfo = this.GetParticipantInfoFromConversation(e.Conversation);
                handler(sender, new ConversationEventArgs(e.Conversation, contactInfo, conversationType));
            }
        }

        private void ConversationManagerConversationAdded(object sender, ConversationManagerEventArgs e)
        {
            var handler = this.ConversationStarted;
            if (handler != null)
            {
                var conversationType = this.activeConversationsToConversationTypeMap[e.Conversation];
                var contactInfo = this.GetParticipantInfoFromConversation(e.Conversation);
                handler(sender, new ConversationEventArgs(e.Conversation, contactInfo, conversationType));
            }
        }

        private ContactInfo GetParticipantInfoFromConversation(Conversation conversation)
        {
            var participant = conversation.Participants.First(x => !x.IsSelf);
            var contactInfo = participant != null ? ContactInfo.GetContactInfo(participant.Contact) : null;

            return contactInfo;
        }
    }
}
