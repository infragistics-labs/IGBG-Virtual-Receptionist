using System;
using System.Linq;
using System.Windows;
using IGBGVirtualReceptionist.LyncCommunication;
using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;
using Microsoft.Lync.Model.Conversation.AudioVideo;
using System.Windows.Controls;

namespace IGBGVirtualReceptionist
{
    /// <summary>
    /// Interaction logic for ConversationWindow.xaml
    /// </summary>
    public partial class ConversationWindow : Window
    {
        private LyncClient client;
        private Conversation conversation;
        private ConversationType conversationType;

        //self participant's AvModality
        private AVModality avModality;

        // TODO: conversation code should be moved to LyncService
        //self participant's channels
        private AudioChannel audioChannel;
        private VideoChannel videoChannel;

        public ContactInfo Contact { get; private set; }

        public ConversationWindow(Conversation conversation, LyncClient client, ContactInfo contact, ConversationType conversationType)
        {
            InitializeComponent();

            //this.ApplyThemes();

            this.client = client;
            this.conversation = conversation;
            this.Contact = contact;
            this.conversationType = conversationType;

            this.Title = contact.DisplayName;

            InitializeConversation();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            //need to remove event listeners otherwide events may be received after the form has been unloaded
            conversation.StateChanged -= this.ConversationStateChanged;
            avModality.ActionAvailabilityChanged -= this.AvModalityActionAvailabilityChanged;
            avModality.ModalityStateChanged -= this.AvModalityModalityStateChanged;
            audioChannel.StateChanged -= this.AudioChannelStateChanged;
            videoChannel.StateChanged -= this.VideoChannelStateChanged;

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

            //show the current conversation and modality states in the UI
            this.SetConversationStatus(conversation.State.ToString());
            this.SetModalityStatus(avModality.State.ToString());
            this.SetAudioStatus("Disconnected");
            this.SetVideoStatus("Disconnected");

            //registers for conversation state updates
            conversation.StateChanged += this.ConversationStateChanged;
            //subscribes to modality action availability events (all audio button except DTMF)
            avModality.ActionAvailabilityChanged += this.AvModalityActionAvailabilityChanged;
            //subscribes to the modality state changes so that the status bar gets updated with the new state
            avModality.ModalityStateChanged += this.AvModalityModalityStateChanged;
            //subscribes to the video channel state changes so that the status bar gets updated with the new state
            audioChannel.StateChanged += this.AudioChannelStateChanged;
            //subscribes to the video channel state changes so that the video feed can be presented
            videoChannel.StateChanged += this.VideoChannelStateChanged;
        }
 
        private void InitiateAudioCall()
        {
            //starts an audio call or conference by connecting the AvModality
            try
            {
                AsyncCallback callback = new AsyncOperationHandler(avModality.EndConnect).Callback;
                avModality.BeginConnect(callback, null);
            }
            catch (LyncClientException lyncClientException)
            {
                Console.WriteLine("ConversationWindow Error:" + lyncClientException);
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

        private void InitiateVideoCall()
        {
            //starts a video call or the video stream in a audio call
            try
            {
                AsyncCallback callback = new AsyncOperationHandler(videoChannel.EndStart).Callback;
                videoChannel.BeginStart(callback, null);
            }
            catch (LyncClientException lyncClientException)
            {
                Console.WriteLine("ConversationWindow Error:" + lyncClientException);
            }
            catch (SystemException systemException)
            {
                if (LyncModelExceptionHelper.IsLyncException(systemException))
                {
                    // Log the exception thrown by the Lync Model API.
                    Console.WriteLine("ConversationWindow Error:" + systemException);
                }
                else
                {
                    // Rethrow the SystemException which did not come from the Lync Model API.
                    throw;
                }
            }
        }

        private void ApplyThemes()
        {
            var assemblyFullName = this.GetType().Assembly.FullName;
            var sourceUri = new Uri(string.Format("pack://application:,,,/{0};component/{1}", assemblyFullName, @"Themes\IG.MSControls.Core.Implicit.xaml"));

            this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = sourceUri });
            sourceUri = new Uri(string.Format("pack://application:,,,/{0};component/{1}", assemblyFullName, @"Themes\IG.xamTileManager.xaml"));

            this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = sourceUri });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            switch (this.conversationType)
            {
                case ConversationType.Audio:
                    this.InitiateAudioCall();
                    break;
                case ConversationType.Text:
                    break;
                case ConversationType.Video:
                    this.InitiateVideoCall();
                    break;
            }
        }

        private void SetModalityStatus(string status)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                this.modalityStatus.Text = "Modality: " + status;
            }));
        }

        private void SetAudioStatus(string status)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                this.audioStatus.Text = "Audio: " + status;
            }));
        }

        private void SetVideoStatus(string status)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                this.videoStatus.Text = "Video: " + status;
            }));
        }

        private void SetConversationStatus(string status)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                this.conversationStatus.Text = status;
            }));
        }

        private void ConversationStateChanged(object sender, ConversationStateChangedEventArgs e)
        {
            this.SetConversationStatus(e.NewState.ToString());
        }

        private void AvModalityModalityStateChanged(object sender, ModalityStateChangedEventArgs e)
        {
            this.SetModalityStatus(e.NewState.ToString());
        }

        private void AvModalityActionAvailabilityChanged(object sender, ModalityActionAvailabilityChangedEventArgs e)
        {
            this.SetModalityStatus(e.Action.ToString());

            // TODO: take some actions depending ot the modality action
            switch (e.Action)
            {
                case ModalityAction.Connect:
                    break;
                case ModalityAction.Disconnect:
                    break;
                case ModalityAction.Hold:
                    break;
                case ModalityAction.Retrieve:
                    break;
                case ModalityAction.LocalTransfer:
                    break;
                case ModalityAction.ConsultAndTransfer:
                    break;
                case ModalityAction.Forward:
                    break;
                case ModalityAction.Accept:
                    break;
                case ModalityAction.Reject:
                    break;
            }
        }

        private void AudioChannelStateChanged(object sender, ChannelStateChangedEventArgs e)
        {
            this.SetAudioStatus(e.NewState.ToString());
        }

        private void VideoChannelStateChanged(object sender, ChannelStateChangedEventArgs e)
        {
            this.SetVideoStatus(e.NewState.ToString());

            //*****************************************************************************************
            //                              Video Content
            //
            // The video content is only available when the Lync client is running in UISuppressionMode.
            //
            // The video content is not directly accessible as a stream. It's rather available through
            // a video window that can de drawn in any panel or window.
            //
            // The outgoing video is accessible from videoChannel.CaptureVideoWindow
            // The window will be available when the video channel state is either Send or SendReceive.
            // 
            // The incoming video is accessible from videoChannel.RenderVideoWindow
            // The window will be available when the video channel state is either Receive or SendReceive.
            //
            //*****************************************************************************************

            // TODO: this one is working only if the app is located somewhere in user's directory due to some Windows user permissions.

            ////if the outgoing video is now active, show the video (which is only available in UI Suppression Mode)
            //if ((e.NewState == ChannelState.Send || e.NewState == ChannelState.SendReceive) &&
            //    videoChannel.CaptureVideoWindow != null)
            //{
            //    //presents the video in the panel
            //    //ShowVideo(this.outVideo, videoChannel.CaptureVideoWindow);
            //    ShowVideo(this.outVideo, videoChannel.CaptureVideoWindow);
            //}

            //if the incoming video is now active, show the video (which is only available in UI Suppression Mode)
            if ((e.NewState == ChannelState.Receive || e.NewState == ChannelState.SendReceive) &&
                videoChannel.RenderVideoWindow != null)
            {
                //presents the video in the panel
                //ShowVideo(this.inVideo, videoChannel.RenderVideoWindow);
                ShowVideo(this.inVideo, videoChannel.RenderVideoWindow);
            }
        }

        /// <summary>
        /// Shows the specified video window in the specified panel.
        /// </summary>
        private void ShowVideo(Panel videoPanel, VideoWindow videoWindow)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                try
                {
                    var window = new VideoWindowHost(videoWindow, 300, 300);
                    videoPanel.Children.Add(window);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("ConversationWindow Error:" + exception);
                }
            }));
        }

        //private void ShowVideo(System.Windows.Forms.Panel videoPanel, VideoWindow videoWindow)
        //{
        //    //Win32 constants:                  WS_CHILD | WS_CLIPCHILDREN | WS_CLIPSIBLINGS;
        //    const long lEnableWindowStyles = 0x40000000L | 0x02000000L | 0x04000000L;
        //    //Win32 constants:                   WS_POPUP| WS_CAPTION | WS_SIZEBOX
        //    const long lDisableWindowStyles = 0x80000000 | 0x00C00000 | 0x00040000L;
        //    const int OATRUE = -1;

        //    Dispatcher.BeginInvoke((Action)(() =>
        //    {
        //        try
        //        {
        //            // TODO: setting videoWindow.Owner throws an exception. Fix it

        //            ////sets the properties required for the native video window to draw itself
        //            //videoWindow.Owner = videoPanel.Handle.ToInt32();
        //            //videoWindow.SetWindowPosition(0, 0, videoPanel.Width, videoPanel.Height);

        //            ////gets the current window style to modify it
        //            //long currentStyle = videoWindow.WindowStyle;

        //            ////disables borders, sizebox, close button
        //            //currentStyle = currentStyle & ~lDisableWindowStyles;

        //            ////enables styles for a child window
        //            //currentStyle = currentStyle | lEnableWindowStyles;

        //            ////updates the current window style
        //            //videoWindow.WindowStyle = (int)currentStyle;

        //            //updates the visibility
        //            videoWindow.Visible = OATRUE;

        //            videoWindow.Width = 300;
        //            videoWindow.Height = 300;
        //        }
        //        catch (Exception exception)
        //        {
        //            Console.WriteLine("ConversationWindow Error:" + exception);
        //        }
        //    }));
        //}
    }
}
