using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Configuration;

namespace hmTextSearcher
{
    /// <summary>
    /// Interaction logic for SearchStart.xaml
    /// </summary>
    public partial class SearchStart : Window
    {
        private SearchOptions so { get; set; }

        public SearchStart()
        {
            InitializeComponent();

            this.InitConnections();
        }

        private void InitConnections()
        {
            // get connection strings
            this.cmb_connection.ItemsSource = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).ConnectionStrings.ConnectionStrings;
            this.cmb_connection.DisplayMemberPath = "Name";
            this.cmb_connection.SelectedValuePath = "ConnectionString";           
        }

        public SearchStart(SearchOptions so) : this()
        {
            this.so = so;

            this.cmb_searchType.SelectionChanged += this.cmb_searchType_SelectionChanged;
            this.cmb_searchType_SelectionChanged(null, null);
        }

        private void cmb_searchType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.cmb_searchType.SelectedIndex == 0)
            {
                // load info about hdd search
                this.gb_connection.Visibility = Visibility.Collapsed;
                this.tb_help.Text = File.OpenText("..\\..\\Help\\hddRU.txt").ReadToEndAsync().Result;
                this.so.Type = SearchOptions.SearchType.HDD;
            }
            else
            {
                // load info about searching in db
                this.gb_connection.Visibility = Visibility.Visible;
                this.tb_help.Text = File.OpenText("..\\..\\Help\\dbRU.txt").ReadToEndAsync().Result;
                this.so.Type = SearchOptions.SearchType.DataBase;                
            }
        }

        private void cmb_connection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int passStart = this.cmb_connection.SelectedItem.ToString().IndexOf("PWD");

            // hide password in info-textbox
            this.tb_help.Text = passStart != -1 ?
            this.cmb_connection.SelectedItem.ToString().Remove(passStart + 4).Insert(passStart + 4, "****") :
            this.cmb_connection.SelectedItem.ToString();
        }

        private void but_newConnection_Click(object sender, RoutedEventArgs e)
        {
            // add connection string to %.exe.Config file
            ConnectionString cs = new ConnectionString();
            if (cs.ShowDialog() == true)
                this.InitConnections();
        }

        // collect information about choosen settings of search
        private void but_start_Click(object sender, RoutedEventArgs e)
        {
            if (this.cmb_searchType.SelectedIndex != 0)
            {
                // database choosen
                if (this.cmb_connection.SelectedIndex != -1)
                {
                    so.Type = SearchOptions.SearchType.DataBase;
                    so.ConnString = this.cmb_connection.SelectedValue?.ToString();
                }
                else
                {
                    MessageBox.Show("Choose connection!");
                    return;
                }

            }

            this.DialogResult = true;
            this.Close();
        }
    }
}
