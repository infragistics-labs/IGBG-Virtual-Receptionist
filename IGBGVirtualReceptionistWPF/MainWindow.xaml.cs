using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using IGBGVirtualReceptionist.LyncCommunication;
using System.Timers;
using Microsoft.Lync.Model;

namespace IGBGVirtualReceptionist
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private LyncService lyncService;

        private List<ContactInfo> currentSearchResults = new List<ContactInfo>();

        public MainWindow()
        {
            InitializeComponent();

            ApplyThemes();
            Persons = new ObservableCollection<Person>();

            Persons.Add(new Person() { Name = "John Smith", Age = 20 });
            Persons.Add(new Person() { Name = "Patty Smith", Age = 20 });
            Persons.Add(new Person() { Name = "John Doe", Age = 20 });
            Persons.Add(new Person() { Name = "Alan White", Age = 20 });
            Persons.Add(new Person() { Name = "Ritchie Brown", Age = 20 });
            Persons.Add(new Person() { Name = "Alex Right", Age = 20 });
            Persons.Add(new Person() { Name = "Mayeble Own", Age = 20 });

            //     SelectedPersons = new List<Person>();
            //     SelectedPersons.Add(new Person() { Name = "Mayeble Own", Age = 20 });
            this.DataContext = this;

            this.lyncService = new LyncService();
            this.lyncService.ClientStateChanged += this.LyncServiceClientStateChanged;
            this.lyncService.SearchContactsFinished += this.LyncServiceSearchContactsFinished;
        }

        private void ApplyThemes()
        {
            var assemblyFullName = this.GetType().Assembly.FullName;
            var sourceUri = new Uri(string.Format("pack://application:,,,/{0};component/{1}", assemblyFullName, @"Themes\IG.MSControls.Core.Implicit.xaml"));

            this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = sourceUri });
            sourceUri = new Uri(string.Format("pack://application:,,,/{0};component/{1}", assemblyFullName, @"Themes\IG.xamTileManager.xaml"));

            this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = sourceUri });

        }

        private void LyncServiceSearchContactsFinished(object sender, SearchContactsEventArgs e)
        {
            // TODO: remove this list if not needed
            //this.currentSearchResults.Clear();
            //this.currentSearchResults.AddRange(e.FoundContacts);

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

        private List<Person> _searchResults;
        public List<Person> SearchResults
        {
            get
            {
                return _searchResults;
            }

            set
            {
                _searchResults = value; NotifyPropertyChanged("SearchResults");
            }
        }
        public ObservableCollection<Person> _persons;
        public ObservableCollection<Person> Persons
        {
            get
            {
                return _persons;
            }

            set
            {
                _persons = value; NotifyPropertyChanged("Persons");
            }
        }
        public List<Person> _selectedPersons;
        public List<Person> SelectedPersons
        {
            get
            {
                return _selectedPersons;
            }

            set
            {
                _selectedPersons = value; NotifyPropertyChanged("SelectedPersons");
            }
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

        //  private void PersonComboEditor_SelectionChanged(object sender, Infragistics.Controls.Editors.SelectionChangedEventArgs e)
        //  {
        //      //if (e.AddedItems.Count > 0)
        //      //    gridPersonDetailsPanel.Visibility = System.Windows.Visibility.Visible;
        //      //else
        //      //    gridPersonDetailsPanel.Visibility = System.Windows.Visibility.Collapsed;
        //      SelectedPersons = new List<Person>();
        //      SelectedPersons.Add(e.AddedItems[0] as Person);

        ////      xamDataCards.DataSource = SelectedPersons;
        // }
    }

    public class MyConv : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
