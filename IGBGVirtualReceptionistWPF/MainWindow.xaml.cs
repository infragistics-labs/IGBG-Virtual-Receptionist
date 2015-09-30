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

namespace IGBGVirtualReceptionist
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
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

            this.DataContext = this;

            var lyncService = new LyncService();
            lyncService.Initialize();
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private void PersonComboEditor_SelectionChanged(object sender, Infragistics.Controls.Editors.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
                gridPersonDetailsPanel.Visibility = System.Windows.Visibility.Visible;
            else
                gridPersonDetailsPanel.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}
