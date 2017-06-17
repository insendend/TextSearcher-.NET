using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace hmTextSearcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SearchOptions so = null;

        private Action<string> addToListBox = null;
        private Action<string> addToTb_filesfound = null;
        private Action<string> addToTb_searchFolder = null;
        private Action<string> addToTb_statusChoosen = null;

        // event for stop status in statusbar
        private AutoResetEvent areStopStatus = null;

        private AutoResetEvent mrePause1 = null,  mrePause2 = null;

        private ManualResetEvent areStop = null;

        public MainWindow()
        {
            InitializeComponent();

            this.addToListBox = s => this.lb_files.Items.Add(s);
            this.addToTb_filesfound = s => this.tb_filesFound.Text = s;
            this.addToTb_searchFolder = s => this.tb_status.Text = s;
            this.addToTb_statusChoosen = s => this.tb_statusChoosenFiles.Text = s;
            this.so = new SearchOptions();
            this.areStopStatus = new AutoResetEvent(false);
            this.mrePause1 = new AutoResetEvent(false);
            this.mrePause2 = new AutoResetEvent(false);
            this.areStop = new ManualResetEvent(false);

            // event of changed items count in the listbox
            ((INotifyCollectionChanged)this.lb_files.Items).CollectionChanged += this.lb_files_CollectionChanged;

            this.EnableCtrls(
                false, 
                this.but_search, 
                this.gp_choosenFiles, 
                this.but_selectAll,
                this.but_open, 
                this.but_unselectAll, 
                this.tbut_pause, 
                this.but_stop);
        }

        private void tb_searchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.sp_skipOption.Visibility =
                string.IsNullOrEmpty(this.tb_searchText.Text) ?
                Visibility.Collapsed :
                Visibility.Visible;
        }

        // change enable status of controls
        private void EnableCtrls(bool isEnable, params Control[] objs)
        {
            foreach (var obj in objs)
                obj.IsEnabled = isEnable;
        }

        private void but_browseFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();

                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    this.tb_path.Text = fbd.SelectedPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void but_search_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SearchStart ss = new SearchStart(so);

                if (ss.ShowDialog() == true)
                {
                    this.lb_files.Items.Clear();
                    this.tb_log.Clear();
                    this.tb_statusChoosenFiles.Visibility = Visibility.Collapsed;
                    this.tb_statusSearch.Text = string.Empty;

                    // enable/disable buttons
                    this.EnableCtrls(false, this.but_search);
                    this.EnableCtrls(true, this.tbut_pause, this.but_stop);

                    this.InitSearchOptions();

                    if (this.so.Type == SearchOptions.SearchType.HDD)
                    {
                        // search in HDD

                        // animated text in status textblock
                        Action actTextAnim = this.TextAnimation;
                        actTextAnim.BeginInvoke(this.FinishAnimation, actTextAnim);

                        // async add filepathes to listbox
                        Action searchHDD = this.SearchInHdd;
                        searchHDD.BeginInvoke(this.FinishListBox, searchHDD);
                    }
                    else
                    {
                        // search in db

                        // async add filepathes to listbox
                        Action searchDb = this.SearchInDatabase;
                        searchDb.BeginInvoke(this.FinishListBox, searchDb);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SearchInDatabase()
        {
            try
            {
                // create db if not exist
                this.CreateDbIfNotExist(this.so.GetDbName());

                DataTable dt = new DataTable();
                SqlDataAdapter sda = new SqlDataAdapter(
                    "select * from profiles",
                    this.so.ConnString);

                sda.Fill(dt);

                // for update
                new SqlCommandBuilder(sda);

                SearchOptions soInDb = new SearchOptions { Type = SearchOptions.SearchType.DataBase };
                int rowOption = 0;

                Action actTextAnim = null;

                // table 'profiles' not empty
                foreach (DataRow row in dt.Rows)
                {
                    // get info from database, table 'profiles'
                    soInDb.Path = row[1].ToString();
                    soInDb.Patterns = row[2].ToString().Split(new char[] { ';', ' ' }, StringSplitOptions.RemoveEmptyEntries); ;
                    soInDb.Text = row[3].ToString();
                    soInDb.isRecursive = (bool)row[4];
                    soInDb.isMatchCase = (bool)row[5];
                    soInDb.MaxSize = (long)row[6];

                    // compare info in 'profiles' with new options
                    if (this.so.Equals(soInDb))
                    {
                        // already exist, ask for replace
                        if (MessageBox.Show(
                            $"Database '{this.so.GetDbName()}' have already had info about your search options:\r\n" +
                            $"Path: {soInDb.Path}\r\n" +
                            $"Patterns: {string.Join("; ", soInDb.Patterns)}\r\n" +
                            $"Text: {soInDb.Text}\r\n" +
                            $"Last changes: {row[7]}\r\n" +
                            $"Do you want to UPDATE data?",
                            "Update data",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            // update time in profiles and change table with info

                            row[7] = DateTime.Now;
                            sda.Update(dt).ToString();

                            // animated text in status textblock
                            actTextAnim = this.TextAnimation;
                            actTextAnim.BeginInvoke(this.FinishAnimation, actTextAnim);

                            // add info
                            this.InsertResults($"Table{row[0]}");
                        }

                        // search in db in the table with info and ouput into the listbox

                        // show at the listbox (from db)
                        this.ShowFromTable($"Table{row[0]}");

                        return;
                    }
                    // last row in the table 'profiles'
                    rowOption = (int)row[0];
                }

                // animated text in status textblock
                actTextAnim = this.TextAnimation;
                actTextAnim.BeginInvoke(this.FinishAnimation, actTextAnim);

                // add new options
                this.InsertOptions();

                // create table and insert info
                this.InsertResults($"Table{++rowOption}");

                // show at the listbox (from db)
                this.ShowFromTable($"Table{rowOption}");

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ShowFromTable(string name)
        {
            try
            {
                DataTable dt = new DataTable();
                SqlDataAdapter sda = new SqlDataAdapter(
                    $"select * from {name}",
                    this.so.ConnString);

                sda.Fill(dt);

                int counter = 0;

                foreach (DataRow row in dt.Rows)
                {
                    this.Dispatcher.Invoke(this.addToListBox, row[1].ToString());

                    this.Dispatcher.Invoke(this.addToTb_filesfound,
                        string.Format($"Has been found {++counter} files"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void InsertResults(string nameTable)
        {
            try
            {
                DbProviderFactory fac = null;
                fac = DbProviderFactories.GetFactory("System.Data.SqlClient");

                using (var conn = fac.CreateConnection())
                {
                    conn.ConnectionString = this.so.ConnString;
                    conn.Open();

                    // create table if not exist
                    var comm = conn.CreateCommand();
                    comm.CommandText = this.GetCreateInfoTable(nameTable);
                    comm.ExecuteNonQuery();

                    // delete data from table
                    comm = conn.CreateCommand();
                    comm.CommandText = this.GetDeleteInfoTable(nameTable);
                    comm.ExecuteNonQuery();

                    int counter = 0;

                    foreach (var path in this.GetFiles(ref counter, this.so.Path, this.so.Patterns))
                    {
                        // insert info
                        comm = conn.CreateCommand();
                        comm.CommandText = this.GetInsertInfoTable(nameTable);

                        // path params
                        var param = comm.CreateParameter();
                        param.ParameterName = "@Path";
                        param.Value = path;
                        param.DbType = System.Data.DbType.String;
                        comm.Parameters.Add(param);

                        comm.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private string GetDeleteInfoTable(string nameTable)
        {
            return $"delete from {nameTable}";
        }

        private List<string> GetFiles(ref int counter, string path, string[] patterns)
        {
            var files = new List<string>();

            try
            {
                // pause
                if (this.mrePause1.WaitOne(1))
                {
                    this.Dispatcher.Invoke(() => this.tb_statusSearch.Text = "Search has been paused.");
                    this.mrePause2.WaitOne();
                }

                // stop
                if (this.areStop.WaitOne(1))
                {
                    this.Dispatcher.Invoke(() => this.tb_statusSearch.Text = "Search has been aborted.");
                    return null;
                }

                if (this.so.isRecursive)
                    foreach (var directory in Directory.GetDirectories(path))
                        files.AddRange(GetFiles(ref counter, directory, patterns));

                foreach (var pattern in patterns)

                    foreach (var file in Directory.GetFiles(path, pattern))
                    {
                        if (string.IsNullOrEmpty(this.so.Text))
                        {
                            files.Add(file);
                            this.Dispatcher.Invoke(this.addToTb_filesfound,
                                string.Format($"Has been found {++counter} files"));
                        }

                        else
                        {
                            // search text in the files
                            if (new FileInfo(file).Length < this.so.MaxSize)

                                using (StreamReader sr = File.OpenText(file))

                                    while (!sr.EndOfStream)
                                    {
                                        string strLine = sr.ReadLine();

                                        if (this.so.isMatchCase ?
                                            strLine.Contains(this.so.Text) :
                                            strLine.ToLower().Contains(this.so.Text.ToLower()))
                                        {
                                            files.Add(file);
                                            this.Dispatcher.Invoke(this.addToTb_filesfound,
                                                string.Format($"Has been found {++counter} files"));
                                            break;
                                        }
                                    }
                        }
                    }
            }
            catch (Exception ex)
            {
                this.AddTextToLog(ex.Message);
            }

            return files;
        }

        private string GetInsertInfoTable(string nameTable)
        {
            return $"insert {nameTable} values (@Path)";
        }

        private string GetCreateInfoTable(string name)
        {
            return
                $" if object_id('{name}') is null" +
                $" create table {name}(" +
                " id int primary key identity(1,1) not null," +
                " path nvarchar(max) not null)";
        }

        private void InsertOptions()
        {
            try
            {
                DbProviderFactory fac = null;
                fac = DbProviderFactories.GetFactory("System.Data.SqlClient");

                using (var conn = fac.CreateConnection())
                {
                    conn.ConnectionString = this.so.ConnString;
                    conn.Open();

                    var comm = conn.CreateCommand();
                    comm.CommandText = this.GetInsertOptions();

                    // path
                    var param = comm.CreateParameter();
                    param.ParameterName = "@Path";
                    param.Value = this.so.Path;
                    param.DbType = System.Data.DbType.String;
                    comm.Parameters.Add(param);

                    // masks
                    param = comm.CreateParameter();
                    param.ParameterName = "@Masks";
                    param.Value = string.Join(";", this.so.Patterns);
                    param.DbType = System.Data.DbType.String;
                    comm.Parameters.Add(param);

                    // text
                    param = comm.CreateParameter();
                    param.ParameterName = "@Text";
                    param.Value = this.so.Text;
                    param.DbType = System.Data.DbType.String;
                    comm.Parameters.Add(param);

                    // recursive
                    param = comm.CreateParameter();
                    param.ParameterName = "@IsRecursive";
                    param.Value = this.so.isRecursive;
                    param.DbType = System.Data.DbType.Boolean;
                    comm.Parameters.Add(param);

                    // match case
                    param = comm.CreateParameter();
                    param.ParameterName = "@IsMatchCase";
                    param.Value = this.so.isMatchCase;
                    param.DbType = System.Data.DbType.Boolean;
                    comm.Parameters.Add(param);

                    // skip size
                    param = comm.CreateParameter();
                    param.ParameterName = "@Skip_size";
                    param.Value = this.so.MaxSize;
                    param.DbType = System.Data.DbType.Int64;
                    comm.Parameters.Add(param);

                    // datetime
                    param = comm.CreateParameter();
                    param.ParameterName = "@Date";
                    param.Value = DateTime.Now;
                    param.DbType = System.Data.DbType.DateTime2;
                    comm.Parameters.Add(param);

                    comm.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private string GetInsertOptions()
        {
            return "insert profiles values (@Path, @Masks, @Text, @IsRecursive, @IsMatchCase, @Skip_size, @Date)";
        }

        private void CreateDbIfNotExist(string name)
        {
            try
            {
                DbProviderFactory fac = null;
                fac = DbProviderFactories.GetFactory("System.Data.SqlClient");

                using (var conn = fac.CreateConnection())
                {
                    conn.ConnectionString = this.so.ConnString.Replace(name, "master");
                    conn.Open();

                    var comm = conn.CreateCommand();
                    comm.CommandText = this.GetCreateDb(name);
                    comm.ExecuteNonQuery();
                }

                using (var conn = fac.CreateConnection())
                {
                    conn.ConnectionString = this.so.ConnString;
                    conn.Open();

                    var comm = conn.CreateCommand();
                    comm.CommandText = this.GetCreateTable("profiles");
                    comm.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private string GetCreateTable(string name)
        {
            return
                $" if object_id('{name}') is null" +
                $" create table {name}(" +
                " id int primary key identity(1,1) not null," +
                " path nvarchar(max) not null," +
                " masks nvarchar(max)," +
                " text text," +
                " isRecursive bit not null," +
                " isMatchCase bit not null," +
                " skip_size bigint not null," +
                " date datetime2(0) not null)";
        }

        private string GetCreateDb(string name)
        {
            return
                $"if db_id ('{name}') is null" +
                $" create database {name}";
        }

        private void FinishAnimation(IAsyncResult ar)
        {
            try
            {
                Action act = (Action)ar.AsyncState;
                act.EndInvoke(ar);

                // status in textbox
                this.Dispatcher.Invoke(this.addToTb_searchFolder, "Searching has been finished.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void InitSearchOptions()
        {
            if (!string.IsNullOrEmpty(this.tb_patterns.Text))
                this.so.Patterns = this.tb_patterns.Text.Split(new char[] { ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            this.so.Path = this.tb_path.Text;
            this.so.isRecursive = this.cb_recursive.IsChecked == true;
            this.so.isMatchCase = this.cb_matchCase.IsChecked == true;
            this.so.Text = this.tb_searchText.Text;
            this.so.MaxSize = this.GetSizeInBytes(this.tb_skipSize.Text);
        }

        private long GetSizeInBytes(string sizeInMb)
        {
            long size;
            if (!long.TryParse(sizeInMb, out size))
                return long.MaxValue;
            else
                return size *= 1024 * 1024;
        }

        // callback for finish add to listbox
        private void FinishListBox(IAsyncResult ar)
        {
            try
            {
                Action act = (Action)ar.AsyncState;
                act.EndInvoke(ar);

                // notification for finish searching
                this.areStopStatus.Set();

                // enable/disable buttons
                this.Dispatcher.Invoke(()=>this.EnableCtrls(true, this.but_search));
                this.Dispatcher.Invoke(() => this.EnableCtrls(false, this.tbut_pause, this.but_stop));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SearchInHdd()
        {
            // counter for count found files
            int count = 0;

            this.AddFiles(ref count, this.so.Path, this.so.Patterns);
        }

        private void AddFiles(ref int counter, string path, string[] patterns)
        {
            try
            {

                // pause
                if (this.mrePause1.WaitOne(1))
                {
                    this.Dispatcher.Invoke(() => this.tb_statusSearch.Text = "Search has been paused.");
                    this.mrePause2.WaitOne();
                }

                // stop
                if (this.areStop.WaitOne(1))
                {
                    this.Dispatcher.Invoke(() => this.tb_statusSearch.Text = "Search has been aborted.");
                    return;
                }

                if (this.so.isRecursive)
                    foreach (var dir in Directory.GetDirectories(path))
                        this.AddFiles(ref counter, dir, patterns);


                foreach (var pattern in patterns)

                    foreach (var file in Directory.GetFiles(path, pattern))
                    {
                        if (string.IsNullOrEmpty(this.so.Text))
                        {
                            // search for all files 
                            this.Dispatcher.Invoke(this.addToListBox, file);
                            this.Dispatcher.Invoke(this.addToTb_filesfound,
                                string.Format($"Has been found {++counter} files"));
                        }
                        else
                        {
                            // search text in the files
                            if (new FileInfo(file).Length < this.so.MaxSize)

                                using (StreamReader sr = File.OpenText(file))
                                
                                    while (!sr.EndOfStream)
                                    {
                                        string strLine = sr.ReadLine();

                                        if (this.so.isMatchCase ?
                                            strLine.Contains(this.so.Text) :
                                            strLine.ToLower().Contains(this.so.Text.ToLower()))
                                        {
                                            this.Dispatcher.Invoke(this.addToListBox, file);
                                            this.Dispatcher.Invoke(this.addToTb_filesfound,
                                                string.Format($"Has been found {++counter} files"));
                                            break;
                                        }
                                    }                               
                        }
                    }
            }
            catch (UnauthorizedAccessException ex)
            {
                this.AddTextToLog(ex.Message);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // text in status textblock
        private void TextAnimation()
        {
            while (!areStopStatus.WaitOne(0))
            {
                this.Dispatcher.Invoke(this.addToTb_searchFolder, "Searching.");
                Thread.Sleep(300);
                this.Dispatcher.Invoke(this.addToTb_searchFolder, "Searching..");
                Thread.Sleep(300);
                this.Dispatcher.Invoke(this.addToTb_searchFolder, "Searching...");
                Thread.Sleep(300);
            }

        }

        // async append text to textbox
        private void AddTextToLog(string str)
        {
            Action<string> actAdd = (s) => this.tb_log.AppendLogInfo(s);
            this.Dispatcher.Invoke(actAdd, str);
        }

        private void but_selectAll_Click(object sender, RoutedEventArgs e)
        {
            this.lb_files.SelectAll();
            this.lb_files.Focus();
        }

        private void but_unselectAll_Click(object sender, RoutedEventArgs e)
        {
            this.lb_files.UnselectAll();
        }

        // changed count of items in the listbox
        private void lb_files_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                this.but_selectAll.IsEnabled =
                this.but_unselectAll.IsEnabled =
                this.lb_files.Items.IsEmpty ? false : true;
            }
        }

        private void lb_files_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.gp_choosenFiles.IsEnabled =
            this.but_open.IsEnabled = 
                this.lb_files.SelectedItems.Count != 0 ?
                true :
                false;
        }

        private void tb_path_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.but_search.IsEnabled =
                this.tb_path.Text.Length != 0 ?
                true :
                false;
        }

        private void but_copy_Click(object sender, RoutedEventArgs e)
        {
            string butName = (sender as Button).Name;
            this.tb_statusChoosenFiles.Visibility = Visibility.Visible;
            Action<string> act = this.CopyMoveDelete;
            act.BeginInvoke(butName, this.FinishActionChosenFiles, act);
        }

        
        private void FinishActionChosenFiles(IAsyncResult ar)
        {
            try
            {
                var act = (Action<string>)ar.AsyncState;
                act.EndInvoke(ar);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CopyMoveDelete(string butName)
        {
            // collection of selected files
            var files = new List<FileInfo>();
            foreach (string path in this.Dispatcher.Invoke(() => this.lb_files.SelectedItems))
                files.Add(new FileInfo(path));

            try
            {
                bool isRemove = false;
                int cntFiles = 0;
                

                if (butName == "but_copy" || butName == "but_move")
                {
                    string selectedPath = string.Empty;

                    this.Dispatcher.Invoke( () =>
                   {
                       System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                       if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                           return;
                       fbd.RootFolder = Environment.SpecialFolder.MyComputer;
                       fbd.ShowNewFolderButton = true;
                       selectedPath = fbd.SelectedPath;
                   });

                    if (butName == "but_move" && MessageBox.Show(
                        $"Caution! {files.Count} files will be MOVED!\r\n" +
                        "Do you agree?",
                        "Warning, moving files", MessageBoxButton.OKCancel,
                        MessageBoxImage.Warning) == MessageBoxResult.OK)
                        
                        // action with choosen files
                        foreach (var file in files)
                        {
                            if (butName == "but_copy")
                            {
                                // copy
                                file.CopyTo(Path.Combine(selectedPath, Path.GetFileName(file.FullName)));
                                this.Dispatcher.Invoke(this.addToTb_statusChoosen, $"Copied {++cntFiles} files.");
                            }
                            else
                            {
                                // move
                                file.MoveTo(Path.Combine(selectedPath, Path.GetFileName(file.FullName)));
                                this.Dispatcher.Invoke(this.addToTb_statusChoosen, $"Moved {++cntFiles} files.");

                                isRemove = true;
                            }
                        }                
                }
                else
                    // delete
                    if (MessageBox.Show(
                            $"Caution! {files.Count} files will be DELETED!\r\n" +
                            "Do you agree?",
                            "Warning, deleting files", MessageBoxButton.OKCancel,
                            MessageBoxImage.Warning) == MessageBoxResult.OK)

                    foreach (var file in files)
                    {
                        file.Delete();
                        this.Dispatcher.Invoke(this.addToTb_statusChoosen, $"Deleted {++cntFiles} files.");
                        isRemove = true;
                    }

                // edit listbox items which were moved
                if (isRemove)
                {
                    while (this.Dispatcher.Invoke(() => this.lb_files.SelectedIndex != -1))
                        this.Dispatcher.Invoke(() =>
                        this.lb_files.Items.RemoveAt(
                            this.Dispatcher.Invoke(() => this.lb_files.SelectedIndex)));
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // pause button
        private void tbut_pause_Click(object sender, RoutedEventArgs e)
        {
            if (this.tbut_pause.IsChecked == true)
            {
                this.tbut_pause.Content = "Resume";
                this.tb_statusSearch.Text = "Search was paused...";
                this.mrePause1.Set();                    
            }
            else
            {
                this.mrePause2.Set();
                this.tbut_pause.Content = "Pause";
                this.tb_statusSearch.Text = string.Empty;              
            }
        }

        private void but_stop_Click(object sender, RoutedEventArgs e)
        {
            this.areStop.Set();
            this.areStopStatus.Set();
        }

        private void lb_files_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var item = ItemsControl.ContainerFromElement(this.lb_files, e.OriginalSource as DependencyObject) as ListBoxItem;

                if (item != null)
                {
                    int i = item.ToString().IndexOf(": ");
                    if (i != -1)
                        Process.Start(item.ToString().Substring(i + ": ".Length));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void but_open_Click(object sender, RoutedEventArgs e)
        {
            foreach (string file in this.lb_files.SelectedItems)
                Process.Start(file);
        }
    }

    public static class TextBoxExtentions
    {
        // ext method for append text to textbox 
        public static void AppendLogInfo(this TextBox tb, string text)
        {
            tb.AppendText($"[{DateTime.Now.ToLongTimeString()}] {text}" + Environment.NewLine);
        }
    }
}
