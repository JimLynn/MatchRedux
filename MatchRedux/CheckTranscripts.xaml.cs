using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Net;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Threading.Tasks;
using System.IO;
using Path = System.IO.Path;
using System.Collections.ObjectModel;
using Excel;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using MSExcel = Microsoft.Office.Interop.Excel;

namespace MatchRedux
{
    /// <summary>
    /// Interaction logic for CheckTranscripts.xaml
    /// </summary>
    public partial class CheckTranscripts : Window
    {
        public CheckTranscripts()
        {
            InitializeComponent();
        }
    }
    public class CheckTranscriptsViewModel : INotifyPropertyChanged
    {
        public CheckTranscriptsViewModel()
        {
            Initialise();
        }

        private async void Initialise()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                ReduxThing[] data = new ReduxThing[]
                {
                    new ReduxThing(true)
                    { ExcelChecked = true,
                        Diskref = "5573254790443234884",
                        ReduxChecked = true,
                        IsValid = true,
                        ExcelFail = false,
                        Description = "RINGSTONE ROUND",
                        Title = "FUN TIMES",
                        CheckingState = "Excel Checked OK"
                    },
                    new ReduxThing(true)
                    { ExcelChecked = true,
                        Diskref = "5573254790443234884",
                        ReduxChecked = true,
                        Description = "RINGSTONE ROUND",
                        Title = "FUN TIMES",
                        IsValid = false,
                        ExcelFail = false,
                        CheckingState = "Excel Checked OK"
                    },
                    new ReduxThing(true)
                    { ExcelChecked = true,
                        Diskref = "5573254790443234884",
                        ReduxChecked = false,
                        Description = "RINGSTONE ROUND",
                        Title = "FUN TIMES",
                        IsValid = true,
                        ExcelFail = true,
                        CheckingState = "Failed to find header"
                    },
                    new ReduxThing(true)
                    { ExcelChecked = true,
                        Diskref = "5573254790443234884",
                        ReduxChecked = false,
                        Description = "RINGSTONE ROUND",
                        Title = "FUN TIMES",
                        IsValid = false,
                        ExcelFail = true,
                        CheckingState = "Failed to read excel file"
                    }
                };

                foreach (var item in data)
                {
                    Items.Add(item);
                }
            }
            else
            {
                _saveReport = new RelayCommand(DoSaveReport, CanDoSaveReport);
                await EnsureLoginToken();
                ReduxThing.LoginToken = loginToken;
                //DataContext = this;
                PopulateList();
            }
        }

        private ObservableCollection<ReduxThing> _items = new ObservableCollection<ReduxThing>();
        public ObservableCollection<ReduxThing> Items
        {
            get
            {
                return _items;
            }
            set
            {
                _items = value;
                FirePropertyChanged("Items");
            }
        }

        private void PopulateList()
        {
            var transcripts = Directory.GetFiles(@"C:\Users\James\Downloads\BBC Batch 1A  - 11th March");
            foreach (var path in transcripts)
            {
                Items.Add(new ReduxThing(path));
            }

            foreach (var item in Items)
            {
                item.PropertyChanged += new PropertyChangedEventHandler(item_PropertyChanged);
            }
        }

        void item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var thing = (ReduxThing)sender;
            if (e.PropertyName == "ExcelChecked" || e.PropertyName == "ReduxChecked")
            {
                if (thing.ReduxChecked && thing.ExcelChecked)
                {
                    if (Items.Any(i=>i.ReduxChecked == false))
                    {
                        return;
                    }
                    IsReportReady = true;
                }
            }
        }
        private bool _isReportReady = false;
        public bool IsReportReady
        {
            get
            {
                return _isReportReady;
            }
            set
            {
                _isReportReady = value;
                FirePropertyChanged("IsReportReady");
                _saveReport.RaiseCanExecuteChanged();
            }
        }
        
        private string loginToken = null;

        private async Task EnsureLoginToken()
        {
            if (loginToken == null)
            {
                WebClient login = new WebClient();
                string logged = await login.DownloadStringTaskAsync("http://api.bbcredux.com/user/login?username=jimlynn&password=bakedbeans");

                XElement tokenresult = XElement.Parse(logged);
                //OutputLine(tokenresult.ToString());
                /*
                 * <response>
                      <user>
                        <token>2228599-j8Je7m7b21tObsKTawkHAzz20zxPkLqf</token>
                        <id>2215</id>
                        <username>jimlynn</username>
                        <email>jim.lynn@bbc.co.uk</email>
                        <first_name>Jim</first_name>
                        <last_name>Lynn</last_name>
                        <admin>0</admin>
                        <bbc_content>1</bbc_content>
                        <fta_content>0</fta_content>
                      </user>
                    </response>
                 */

                string t = tokenresult.XPathSelectElement("/user/token").Value;
                loginToken = t;
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;
        private void FirePropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        private RelayCommand _saveReport;
        public RelayCommand SaveReport
        {
            get
            {
                return _saveReport;
            }
            set
            {
                _saveReport = value;
                FirePropertyChanged("SaveReport");
            }
        }

        private bool CanDoSaveReport()
        {
            return IsReportReady;
        }

        private void DoSaveReport()
        {
            var app = new MSExcel.Application();
            app.Visible = true;
            app.Workbooks.Add();

            var worksheet = (MSExcel.Worksheet)app.ActiveSheet;

            worksheet.Cells[1, "A"] = "Report of broken spreadsheets";

            worksheet.Cells[3, "A"] = "Filename";
            worksheet.Cells[3, "B"] = "Matches Redux?";
            worksheet.Cells[3, "C"] = "Format Checks?";

            int rowCounter = 4;
            foreach (var item in Items)
            {
                worksheet.Cells[rowCounter, "A"] = Path.GetFileName(item.ExcelPath);
                worksheet.Cells[rowCounter, "B"] = item.IsValid;
                worksheet.Cells[rowCounter, "C"] = item.CheckingState;

                rowCounter++;
            }
            worksheet.Range["A3", "C" +(rowCounter-1).ToString()].AutoFormat(
                MSExcel.XlRangeAutoFormat.xlRangeAutoFormatClassic2);
        }
    }

    public class ReduxThing : INotifyPropertyChanged
    {
        public static string LoginToken { get; set; }
        public ReduxThing(string path)
        {
            ExcelPath = path;
            string diskref = Path.GetFileNameWithoutExtension(path);
            Diskref = diskref;
            _launchExcel = new RelayCommand(DoLaunchExcel);
            FetchMetadata();
        }
        public ReduxThing(bool isDesign)
        {
        }

        private void FetchMetadata()
        {
            WebClient checkData = new WebClient();
            checkData.DownloadStringCompleted += (o, a) =>
                {
                    if (a.Error == null)
                    {
                        IsValid = true;
                        ReduxChecked = true;
                        var doc = XElement.Parse(a.Result);
                        /*
                         
                         * <response> 
                         *   <programme> 
                         *      <time>02:00:41</time> 
                         *      <date>1977-04-03</date> 
                         *      <channel>archive</channel> 
                         *      <reference>1864434049555179990</reference> 
                         *      <duration>0</duration> 
                         *      <unixtime>228880841</unixtime> 
                         *      <description>X FOR XYLOPHONE</description> 
                         *      <series_crid/> 
                         *      <programme_crid/> 
                         *      <name>AN ABC OF MUSIC</name> 
                         *      <key>2215-1300810983-a1eec8f046496f00f82baab9480ff4f1</key> 
                         *      <media uri="http://g.bbcredux.com/programme/1864434049555179990/download/2215-1300810983-a1eec8f046496f00f82baab9480ff4f1/iphone.mov" type="iPhone"/> 
                         *      <media generated="1" size="302692352" uri="http://g.bbcredux.com/programme/1864434049555179990/download/2215-1300810983-a1eec8f046496f00f82baab9480ff4f1/original/blah.ts" type="original"/> 
                         *      <media uri="http://g.bbcredux.com/programme/1864434049555179990/download/2215-1300810983-a1eec8f046496f00f82baab9480ff4f1/flash.flv" type="flash"/> 
                         *    </programme> 
                         *  </response>
                         */

                        Title = doc.Element("programme").Element("name").Value;
                        Description = doc.Element("programme").Element("description").Value;
                        ItemState = "Successful";
                    }
                    else
                    {
                        ItemState = "Failed";
                        ReduxChecked = true;
                    }
                    CheckingState = "Checking Excel...";
                    CheckExcel();
                };
            string url = string.Format("http://api.bbcredux.com/content/{0}/data?token={1}", Diskref, LoginToken);
            checkData.DownloadStringAsync(new Uri(url, UriKind.Absolute));
        }

        private void CheckExcel()
        {
            using (var stream = File.Open(ExcelPath, FileMode.Open, FileAccess.Read))
            {
                IExcelDataReader excelreader = ExcelReaderFactory.CreateBinaryReader(stream);
                DataSet result = excelreader.AsDataSet();
                if (result == null || result.Tables.Count == 0)
                {
                    ExcelFail = true;
                    ExcelChecked = true;
                    CheckingState = "No table found in excel file (old format?)";
                    return;
                }
                var x = result.Tables[0];
                var rows = x.Rows;

                if (rows.Count < 15)
                {
                    CheckingState = "Not enough rows in spreadsheet";
                    ExcelFail = true;
                    ExcelChecked = true;
                    return;
                }
                if ((CheckExcel(rows, 0,"UKT ID")
                    && CheckExcel(rows,1,"Start Time")
                    && CheckExcel(rows,2,"End Time")
                    && CheckExcel(rows, 3, "Total Time")
                    && CheckExcel(rows, 4, "Active Minutes")
                    && CheckExcel(rows,5,"Word Count")
                    && CheckExcel(rows,6,"MP3 Filename")
                    && CheckExcel(rows,7,"Disk Reference")
                    && CheckExcel(rows,8,"Series Title")
                    && CheckExcel(rows,9,"Programme Title")
                    && CheckExcel(rows,10,"Programme Number")) == false)
                {
                    ExcelFail = true;
                    ExcelChecked = true;
                    return;
                }
                int headerRow = 0;
                bool foundHeaderRow = false;
                for (headerRow = 12; headerRow < rows.Count; headerRow++)
                {
                    if ((CheckExcel(rows, headerRow, "Name")
                    && CheckExcel(rows, headerRow, 1, "In Point")
                    && CheckExcel(rows, headerRow, 2, "Out Point")
                    && CheckExcel(rows, headerRow, 3, "Text")) == true)
                    {
                        foundHeaderRow = true;
                        break;
                    }
                }
                if (foundHeaderRow == false)
                {
                    CheckingState = "Failed to find header row";
                    ExcelFail = true;
                    ExcelChecked = true;
                    return;
                }

                Regex checkTimespan = new Regex(@"\d\d:\d\d:\d\d");

                for (int dataRow = headerRow + 1; dataRow < rows.Count; dataRow++)
                {
                    string speaker = rows[dataRow][0].ToString();
                    string inpoint = rows[dataRow][1].ToString();
                    string outpoint = rows[dataRow][2].ToString();
                    string text = rows[dataRow][3].ToString();

                    if (speaker.Length + inpoint.Length + outpoint.Length + text.Length > 0)
                    {
                        double inpt, outpt;
                        if (double.TryParse(inpoint, out inpt) == false)
                        {
                            CheckingState = string.Format("Bad inpoint '{0}' at row {1}", inpoint, dataRow + 1);
                            ExcelFail = true;
                            ExcelChecked = true;
                            return;
                        }
                        if (double.TryParse(outpoint, out outpt) == false && speaker != "End")
                        {
                            CheckingState = string.Format("Bad outpoint '{0}' at row {1}", outpoint, dataRow + 1);
                            ExcelFail = true;
                            ExcelChecked = true;
                            return;
                        }

                        //if (checkTimespan.IsMatch(inpoint) == false)
                        //{
                        //    CheckingState = "In Point " + inpoint + " Not Valid";
                        //    ExcelFail = true;
                        //    return;
                        //}
                        //if (checkTimespan.IsMatch(outpoint) == false)
                        //{
                        //    CheckingState = "Out Point " + outpoint + " Not Valid";
                        //    ExcelFail = true;
                        //    return;
                        //}
                    }
                }

                CheckingState = "Excel is OK";
                ExcelChecked = true;
                Title = rows[8][1].ToString();
                Description = rows[9][1].ToString();
            }
        }

        private bool _excelFail = false;
        public bool ExcelFail
        {
            get
            {
                return _excelFail;
            }
            set
            {
                _excelFail = value;
                FirePropertyChanged("ExcelFail");
                FirePropertyChanged("VisibleIfExcelFail");
                FirePropertyChanged("VisibleIfExcelOk");
            }
        }

        public Visibility VisibleIfExcelFail
        {
            get
            {
                if (ExcelFail)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }
        public Visibility VisibleIfExcelOk
        {
            get
            {
                if (ExcelFail)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        public Visibility VisibleIfReduxFail
        {
            get
            {
                if (IsValid)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }
        public Visibility VisibleIfReduxOk
        {
            get
            {
                if (IsValid)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }
        private bool CheckExcel(DataRowCollection rows, int row, string testValue)
        {
            try
            {
                if (rows[row][0].ToString().Trim() != testValue)
                {
                    CheckingState = "Failed to match " + testValue;
                    return false;
                }
                return true;
            }
            catch (IndexOutOfRangeException)
            {
                CheckingState = string.Format("Row {0} is missing expected columns", row);
            }
            catch (Exception ex)
            {
                CheckingState = "Exception: " + ex.Message;
            }
            return false;
        }

        private bool CheckExcel(DataRowCollection rows, int row, int col, string testValue)
        {
            if (rows[row][col].ToString().Trim() != testValue)
            {
                CheckingState = "Failed to match " + testValue;
                return false;
            }
            return true;
        }

        private string _title;
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                FirePropertyChanged("Title");
            }
        }

        private string _checkingState;
        public string CheckingState
        {
            get
            {
                return _checkingState;
            }
            set
            {
                _checkingState = value;
                FirePropertyChanged("CheckingState");
            }
        }

        private bool _reduxChecked = false;
        public bool ReduxChecked
        {
            get
            {
                return _reduxChecked;
            }
            set
            {
                _reduxChecked = value;
                FirePropertyChanged("ReduxChecked");
            }
        }

        private bool _excelChecked = false;
        public bool ExcelChecked
        {
            get
            {
                return _excelChecked;
            }
            set
            {
                _excelChecked = value;
                FirePropertyChanged("ExcelChecked");
            }
        }

        private string _excelPath;
        public string ExcelPath
        {
            get
            {
                return _excelPath;
            }
            set
            {
                _excelPath = value;
                FirePropertyChanged("ExcelPath");
            }
        }

        private string _description;
        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                _description = value;
                FirePropertyChanged("Description");
            }
        }

        private string _itemState = "Loading...";
        public string ItemState
        {
            get
            {
                return _itemState;
            }
            set
            {
                _itemState = value;
                FirePropertyChanged("ItemState");
            }
        }

        private RelayCommand _launchExcel;
        public RelayCommand LaunchExcel
        {
            get
            {
                return _launchExcel;
            }
        }

        private void DoLaunchExcel()
        {
            Process.Start(ExcelPath);
        }

        private bool _isValid = false;
        public bool IsValid
        {
            get
            {
                return _isValid;
            }
            set
            {
                _isValid = value;
                FirePropertyChanged("IsValid");
                FirePropertyChanged("VisibilityValid");
                FirePropertyChanged("VisibilityInvalid");
            }
        }
        public Visibility VisibilityValid
        {
            get
            {
                if (IsValid)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        public Visibility VisibilityInvalid
        {
            get
            {
                if (IsValid)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        private string _diskref;
        public string Diskref
        {
            get { return _diskref; }
            set
            {
                _diskref = value;
                FirePropertyChanged("Diskref");
            }
        }

        public string ReduxUri
        {
            get
            {
                return string.Format(@"http://g.bbcredux.com/programme/{0}", Diskref);
            }
        }

        public string ReduxImage
        {
            get
            {
                return string.Format(@"http://g.bbcredux.com/programme/{0}/download/image-640.jpg", Diskref);
            }
        }

        public string ReduxThumbnail
        {
            get
            {
                return string.Format(@"http://g.bbcredux.com/programme/{0}/download/image-74.jpg", Diskref);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void FirePropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
    }

    public class RelayCommand : ICommand
    {
        private Action _execute;
        private Func<bool> _canExecute;

        public RelayCommand(Action execute)
        {
            _execute = execute;
        }

        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged(this, new EventArgs());
            }
        }
        public bool CanExecute(object parameter)
        {
            if (_canExecute != null)
            {
                return _canExecute();
            }
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            if (_execute != null)
            {
                _execute();
            }
        }
    }
}
