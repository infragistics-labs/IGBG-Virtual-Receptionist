using IGBGVirtualReceptionist.LyncCommunication;
using Microsoft.Practices.Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;


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
       //     this.DataContext = this;

            this.lyncService = new LyncService();
            this.lyncService.ClientStateChanged += this.LyncServiceClientStateChanged;
            this.lyncService.SearchContactsFinished += this.LyncServiceSearchContactsFinished;
            this.favTiles.ItemsSource = lyncService.GetFavoriteContacts();
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


        
        
        public DelegateCommand<ContactInfo> CallCommand
        {
            get
            {
                return new DelegateCommand<ContactInfo>(CallAction);
            }
        }

        public void CallAction(ContactInfo hh)
        {

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