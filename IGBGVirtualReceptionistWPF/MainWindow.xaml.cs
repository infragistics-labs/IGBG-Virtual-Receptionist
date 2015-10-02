using IGBGVirtualReceptionist.LyncCommunication;
using Microsoft.Practices.Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Data;
using IGBGVirtualReceptionist.LyncCommunication;
using Microsoft.Lync.Model.Conversation;


namespace IGBGVirtualReceptionist
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private LyncService lyncService;
        public static BitmapImage dummyPic = new BitmapImage();

        private List<ContactInfo> currentSearchResults = new List<ContactInfo>();

        public MainWindow()
        {
            InitializeComponent();
            ApplyThemes();

            this.lyncService = new LyncService();
            this.lyncService.ClientStateChanged += this.LyncServiceClientStateChanged;
            this.lyncService.SearchContactsFinished += this.LyncServiceSearchContactsFinished;
            this.lyncService.ConversationStarted += this.LyncServiceConversationStarted;
            this.lyncService.ConversationEnded += this.LyncServiceConversationEnded;
        }

        private void LyncServiceConversationEnded(object sender, ConversationManagerEventArgs e)
        {
            MessageBox.Show("Conversation ended!");
        }

        private void LyncServiceConversationStarted(object sender, ConversationManagerEventArgs e)
        {
            MessageBox.Show("Conversation started!");

            e.Conversation.End();
        }

        private void ApplyThemes()
        {
            var assemblyFullName = this.GetType().Assembly.FullName;
            var sourceUri = new Uri(string.Format("pack://application:,,,/{0};component/{1}", assemblyFullName, @"Themes\IG.MSControls.Core.Implicit.xaml"));

            this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = sourceUri });
            sourceUri = new Uri(string.Format("pack://application:,,,/{0};component/{1}", assemblyFullName, @"Themes\IG.xamTileManager.xaml"));

            this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = sourceUri });

            dummyPic = new BitmapImage(new Uri(string.Format("pack://application:,,,/{0};component/{1}", assemblyFullName, @"Images\NoPic.bmp")));
        }

        public DelegateCommand<ContactInfo> TextCommand
        {
            get
            {
                return new DelegateCommand<ContactInfo>(TextAction);
            }
        }

        public DelegateCommand<ContactInfo> AudioCommand
        {
            get
            {
                return new DelegateCommand<ContactInfo>(AudioAction);
            }
        }

        public DelegateCommand<ContactInfo> VideoCommand
        {
            get
            {
                return new DelegateCommand<ContactInfo>(VideoAction);
            }
        }

        private void TextAction(ContactInfo contactInfo)
        {
            this.lyncService.StartConversation(contactInfo.SipUri);
        }

        private void AudioAction(ContactInfo contactInfo)
        {
            this.lyncService.StartConversation(contactInfo.SipUri);
        }

        private void VideoAction(ContactInfo contactInfo)
        {
            this.lyncService.StartConversation(contactInfo.SipUri);
        }

        private void LyncServiceSearchContactsFinished(object sender, SearchContactsEventArgs e)
        {
            // populate datasource
            Dispatcher.BeginInvoke((Action)(() =>
            {
                this.xamDataCards.DataSource = null;
                this.xamDataCards.DataSource = e.FoundContacts;
            }));
        }

        private void LyncServiceClientStateChanged(object sender, Microsoft.Lync.Model.ClientStateChangedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                sbiStatus.Content = e.NewState.ToString();

                if (!this.lyncService.InUISuppressedMode || e.NewState == Microsoft.Lync.Model.ClientState.SignedIn)
                {
                    this.favTiles.ItemsSource = this.lyncService.GetFavoriteContacts();
                }
            }));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.lyncService.Dispose();

            base.OnClosing(e);
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            this.lyncService.StartSearchForContactsOrGroups(searchBox.Text);
        }
    }

}