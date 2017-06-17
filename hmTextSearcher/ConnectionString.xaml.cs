using System;
using System.Collections.Generic;
using System.Configuration;
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
using System.Windows.Shapes;

namespace hmTextSearcher
{
    /// <summary>
    /// Interaction logic for ConnectionString.xaml
    /// </summary>
    public partial class ConnectionString : Window
    {
        public ConnectionString()
        {
            InitializeComponent();

            this.cmb_connType.SelectionChanged += this.cmb_connType_SelectionChanged;
        }

        private void cmb_connType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.sp_uid.Visibility = 
                this.cmb_connType.SelectedIndex == 0 ? 
                Visibility.Collapsed : 
                Visibility.Visible;
        }

        private void but_addConnection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // get the count of the connection strings
                int connStrCnt = ConfigurationManager.ConnectionStrings.Count;

                // define the string name
                string csName = string.IsNullOrEmpty(this.tb_name.Text) ?
                    "ConnStr" + connStrCnt.ToString() :
                    this.tb_name.Text;

                // get the configuration file
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                // add the connection string.
                config.ConnectionStrings.ConnectionStrings.Add(
                    new ConnectionStringSettings(
                        csName,
                        this.ConnString(),
                        "System.Data.SqlClient"));

                // save the configuration file
                config.Save(ConfigurationSaveMode.Modified);

                MessageBox.Show("Connection string added.");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            this.DialogResult = true;
            this.Close();
        }

        private string ConnString()
        {
            string strConnection = string.Empty;

            strConnection = $"Data Source=tcp:{this.tb_serverName.Text}; Initial Catalog={this.tb_dbName.Text}; ";

            if (this.cmb_connType.SelectedIndex == 0)
                strConnection += "Integrated Security=true";
            else
            {
                strConnection += "Integrated Security=false; ";
                strConnection += $"UID={this.tb_login.Text}; ";
                strConnection += $"PWD={this.pb_password.Password}";
            }

            return strConnection;
        }
    }
}
