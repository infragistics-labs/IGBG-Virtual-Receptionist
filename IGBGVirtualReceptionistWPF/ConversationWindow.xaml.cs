using System;
using System.Linq;
using System.Windows;
using IGBGVirtualReceptionist.LyncCommunication;
using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;
using Microsoft.Lync.Model.Conversation.AudioVideo;

namespace IGBGVirtualReceptionist
{
    /// <summary>
    /// Interaction logic for ConversationWindow.xaml
    /// </summary>
    public partial class ConversationWindow : Window
    {
        private LyncClient client;
        private Conversation conversation;

        //self participant's AvModality
        private AVModality avModality;

        //self participant's channels
        private AudioChannel audioChannel;
        private VideoChannel videoChannel;

        public ContactInfo Contact { get; private set; }

        public ConversationWindow(Conversation conversation, LyncClient client, ContactInfo contact)
        {
            InitializeComponent();

            //this.ApplyThemes();

            this.client = client;
            this.conversation = conversation;
            this.Contact = contact;

            this.Title = contact.DisplayName;

            InitializeConversation();
        }

        public void InitiateAudioCall()
        {
            
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            //need to remove event listeners otherwide events may be received after the form has been unloaded
            //conversation.StateChanged -= conversation_StateChanged;
            //conversation.ParticipantAdded -= conversation_ParticipantAdded;
            //conversation.ParticipantRemoved -= conversation_ParticipantRemoved;
            //conversation.ActionAvailabilityChanged -= conversation_ActionAvailabilityChanged;
            //avModality.ActionAvailabilityChanged -= avModality_ActionAvailabilityChanged;
            //avModality.ModalityStateChanged -= avModality_ModalityStateChanged;
            //audioChannel.ActionAvailabilityChanged -= audioChannel_ActionAvailabilityChanged;
            //audioChannel.StateChanged -= audioChannel_StateChanged;
            //videoChannel.ActionAvailabilityChanged -= videoChannel_ActionAvailabilityChanged;
            //videoChannel.StateChanged -= videoChannel_StateChanged;

            //if the conversation is active, will end it
            if (conversation.State != ConversationState.Terminated)
            {
                //ends the conversation which will disconnect all modalities
                try
                {
                    conversation.End();
                }
                catch (LyncClientException lyncClientException)
                {
                    Console.WriteLine("ConversationWindow Error: " + lyncClientException);
                }
                catch (SystemException systemException)
                {
                    if (LyncModelExceptionHelper.IsLyncException(systemException))
                    {
                        // Log the exception thrown by the Lync Model API.
                        Console.WriteLine("ConversationWindow Error: " + systemException);
                    }
                    else
                    {
                        // Rethrow the SystemException which did not come from the Lync Model API.
                        throw;
                    }
                }
            }

            base.OnClosing(e);
        }

        private void InitializeConversation()
        {
            //saves the AVModality, AudioChannel and VideoChannel, just for the sake of readability
            avModality = (AVModality)conversation.Modalities[ModalityTypes.AudioVideo];
            audioChannel = avModality.AudioChannel;
            videoChannel = avModality.VideoChannel;

            // TODO: fix the UI

            ////show the current conversation and modality states in the UI
            //toolStripStatusLabelConvesation.Text = conversation.State.ToString();
            //toolStripStatusLabelModality.Text = avModality.State.ToString();

            ////enables and disables the checkbox associated with the ConversationProperty.AutoTerminateOnIdle property
            ////based on whether the Lync client is running in InSuppressedMode
            ////see more details in the checkBoxAutoTerminateOnIdle_CheckStateChanged() method
            //checkBoxAutoTerminateOnIdle.Enabled = client.InSuppressedMode;

            ////registers for conversation state updates
            //conversation.StateChanged += conversation_StateChanged;

            ////subscribes to the conversation action availability events (for the ability to add/remove participants)
            //conversation.ActionAvailabilityChanged += conversation_ActionAvailabilityChanged;

            ////subscribes to modality action availability events (all audio button except DTMF)
            //avModality.ActionAvailabilityChanged += avModality_ActionAvailabilityChanged;

            ////subscribes to the modality state changes so that the status bar gets updated with the new state
            //avModality.ModalityStateChanged += avModality_ModalityStateChanged;

            ////subscribes to the audio channel action availability events (DTMF only)
            //audioChannel.ActionAvailabilityChanged += audioChannel_ActionAvailabilityChanged;

            ////subscribes to the video channel state changes so that the status bar gets updated with the new state
            //audioChannel.StateChanged += audioChannel_StateChanged;

            ////subscribes to the video channel action availability events
            //videoChannel.ActionAvailabilityChanged += videoChannel_ActionAvailabilityChanged;

            ////subscribes to the video channel state changes so that the video feed can be presented
            //videoChannel.StateChanged += videoChannel_StateChanged;
        }

        private void ApplyThemes()
        {
            var assemblyFullName = this.GetType().Assembly.FullName;
            var sourceUri = new Uri(string.Format("pack://application:,,,/{0};component/{1}", assemblyFullName, @"Themes\IG.MSControls.Core.Implicit.xaml"));

            this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = sourceUri });
            sourceUri = new Uri(string.Format("pack://application:,,,/{0};component/{1}", assemblyFullName, @"Themes\IG.xamTileManager.xaml"));

            this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = sourceUri });
        }
    }
}
