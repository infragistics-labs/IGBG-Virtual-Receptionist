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

        public MainWindow()
        {
            InitializeComponent();
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
            this.lyncService.Initialize();
            lyncService.ClientStateChanged += lyncService_ClientStateChanged;
        }

        void lyncService_ClientStateChanged(object sender, Microsoft.Lync.Model.ClientStateChangedEventArgs e)
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

        public List<ContactInfo> contactInfo = new List<ContactInfo>();

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
         //   contactInfo.Clear();
            //lyncService.results = null;
            //Dispatcher.BeginInvoke((Action)(() =>
            //{
            //    this.lyncService.StartSearchForContactsOrGroups(searchBox.Text);

            //    if (lyncService.results.Count != 0)
            //    {
            //        foreach (var item in lyncService.results)
            //        {
            //            contactInfo.Add(ContactInfo.GetContactInfo(item));

            //        }
            //    }

            //    if (contactInfo.Count > 0)
            //    {
            //        xamDataCards.DataSource = null;
            //        xamDataCards.DataSource = contactInfo;
            //    }
            //}));
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
