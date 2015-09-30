using System.Windows;
using Microsoft.Lync.Model;
using System;
using System.Linq;

namespace IGBGVirtualReceptionist.LyncCommunication
{
    public class LyncService : IDisposable
    {
        private LyncClient client;
        private bool isLyncProcessInitializedByThisApp = false;

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
                    this.isLyncProcessInitializedByThisApp = true;

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

        public void Dispose()
        {
            this.client.StateChanged -= this.Client_StateChanged;
            this.client.ClientDisconnected -= this.Client_ClientDisconnected;

            // TODO: 
            //this.client.ConversationManager.ConversationAdded -= this.ConversationManager_ConversationAdded;
            //this.client.ConversationManager.ConversationRemoved -= this.ConversationManager_ConversationRemoved;
        }

        private void ClientInitialized(IAsyncResult result)
        {
            if (!result.IsCompleted)
            {
                Console.WriteLine("LyncService: Error initializing the LyncClient");
                return;
            }


            //registers for conversation related events
            //these events will occur when new conversations are created (incoming/outgoing) and removed

            // TODO:
            //client.ConversationManager.ConversationAdded += ConversationManager_ConversationAdded;
            //client.ConversationManager.ConversationRemoved += ConversationManager_ConversationRemoved;
        }

        private void Client_StateChanged(object sender, ClientStateChangedEventArgs e)
        {
            var handler = this.ClientStateChanged;
            if (handler != null)
            {
                handler(this, e);
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
    }
}
