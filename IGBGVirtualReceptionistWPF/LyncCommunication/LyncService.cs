using System.Windows;
using Microsoft.Lync.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IGBGVirtualReceptionist.LyncCommunication
{
    public class LyncService : IDisposable
    {
        private LyncClient client;
        private bool thisInitializedLync = false;

        public event EventHandler<ClientStateChangedEventArgs> ClientStateChanged;
        public event EventHandler ClientDisconnected;

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
                        Console.WriteLine("LyncService: " + lyncClientException);
                    }
                    catch (SystemException systemException)
                    {
                        if (LyncModelExceptionHelper.IsLyncException(systemException))
                        {
                            Console.WriteLine("LyncService: " + systemException);
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
                    Console.WriteLine("LyncService: " + ex);
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

        private void ClientInitialized(IAsyncResult result)
        {
            if (!result.IsCompleted)
            {
                Console.WriteLine("LyncService: Error initializing the LyncClient");
                return;
            }

            this.thisInitializedLync = true;
            this.client.EndInitialize(result);

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

        private void Client_CredentialRequested(object sender, CredentialRequestedEventArgs e)
        {
            //If the server type is Lync server and sign in credentials
            //are needed.
            if (e.Type == CredentialRequestedType.SignIn)
            {
                //Re-submit sign in credentials
                e.Submit("oracle@infragistics.com", "", true);
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
    }
}
