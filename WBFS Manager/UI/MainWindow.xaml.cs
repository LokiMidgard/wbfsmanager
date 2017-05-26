using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using libwbfsNET;
using Microsoft.Win32;
using WBFSManager.Data;
using WBFSManager.UI;
using WBFSManager.UI.Commands;
using IOPath = System.IO.Path;
using System.Windows.Markup;
using System.Windows.Documents;
using System.Windows.Media.Effects;

namespace WBFSManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Fields
        /// <summary>
        /// Parameter used by UI commands to indicate the event coming from To Add ListBox
        /// </summary>
        const string TOADDCONTEXTPARAM = "ToAdd";
        /// <summary>
        /// Letter to use as first letter of chan ID when batch creating channels
        /// </summary>
        private const string LEADINGCHANLETTER = "U";
        /// <summary>
        /// Parameter used by UI commands to indicate the event coming from On Drive ListBox
        /// </summary>
        const string ONDRIVECONTEXTPARAM = "OnDrive";
        /// <summary>
        /// Location of boot.dol file for Hombrew Channel entry creation
        /// </summary>
        const string HBCDOLPATH = @"HBC\boot.dol";
        /// <summary>
        /// Location of icon.png file for Hombrew Channel entry creation
        /// </summary>
        const string HBCPNGPATH = @"HBC\icon.png";
        /// <summary>
        /// Original title ID of boot.dol for Homebrew Channel entry creation (not Channel creation!)
        /// </summary>
        const string HBCORIGTITLEID = "RSBE01";
        /// <summary>
        /// Name of language resource files generated from .resx resource files.
        /// </summary>
        private const string WBFSLANGUAGEDLLNAME = "WBFSManager.resources.dll";
        /// <summary>
        /// Progress dialog object, used to open and close the progress dialog box.
        /// </summary>
        ProgressDialog pd = null;
        /// <summary>
        /// Holds a reference to the curerntly checked language item on the language menu.
        /// Whenever the language is changed this should be changed to point to the selected language's menu item.
        /// IsChecked is true for this item. Upon change, IsChecked should be changed back to false and the new selection should be set to IsChecked=true
        /// </summary>
        MenuItem checkedLanguageMenuItem = null;
        /// <summary>
        /// An object of WBFSDrive which is used to do operations on the WBFS drive. Can create a separate object if accessing more than one drive (ex. Drive-To-Drive Transfer)
        /// </summary>
        WbfsDrive wbfsDriveCurrent;
        /// <summary>
        /// An object of WBFSDrive which is used to do operations on the secondary WBFS drive. (Cloning, drive-to-drive).
        /// </summary>
        WbfsDrive wbfsDriveSecondary;
        #endregion Fields

        #region Properties
        /// <summary>
        /// A property bound to by the EstimatedSize field on the main UI. Calculates the estimated total size of the items in the Add To ListBox.
        /// </summary>
        public String EstimatedTotalSizeString
        {
            get
            {
                return EstimatedTotalSize.ToString("N2") + "GB";
            }
        }
        public float EstimatedTotalSize
        {
            get
            {
                float total = 0;
                foreach (WbfsEntry item in wbfsDriveCurrent.EntriesToAdd)
                {
                    total += item.EntrySize;
                }
                return total;
            }
        }
        public float PercentDiskSpaceUsed
        {
            get
            {
                if (wbfsDriveCurrent == null || wbfsDriveCurrent.DriveStats == null)
                    return 0;
                return (wbfsDriveCurrent.DriveStats.UsedSpace * 100f) / wbfsDriveCurrent.DriveStats.TotalSpace;
            }
        }
        public float PercentDiskPlusToAdd
        {
            get
            {
                if (wbfsDriveCurrent == null || wbfsDriveCurrent.DriveStats == null || wbfsDriveCurrent.EntriesToAdd == null || wbfsDriveCurrent.EntriesToAdd.Count < 1)
                    return 0;

                float result = ((wbfsDriveCurrent.DriveStats.UsedSpace + EstimatedTotalSize) * 100f) / wbfsDriveCurrent.DriveStats.TotalSpace;
                if (result >= 90)
                    PercentagePlusToAddBar.Foreground = Brushes.Red;
                else
                    PercentagePlusToAddBar.Foreground = new SolidColorBrush(Color.FromRgb(1, 211, 40));
                return result;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor for the MainWindow
        /// Instantiates wbfsDriveCurrent, sets the sources for the listBoxes
        /// Sets red glow on format button, sets the ListBox's sort style (to by Name), creates the Language submenu.
        /// </summary>
        public MainWindow()
        {
            wbfsDriveCurrent = new WbfsDrive();     //Instantiate wbfsDriveCurrent to avoid issues with bindings getting null values
            wbfsDriveSecondary = new WbfsDrive();   //Instantiate wbfsDriveSecondary to avoid issues with bindings getting null values
            InitializeComponent();
#if DEBUG
            TestButton.Visibility = Visibility.Visible;
#endif
            EntryListBox.ItemsSource = wbfsDriveCurrent.EntriesOnDrive;         //Set the source for the entries on drive list box (left-hand)
            AddListBox.ItemsSource = wbfsDriveCurrent.EntriesToAdd;             //St the source for the entries to add list box (right-hand)
            SecondDriveListBox.ItemsSource = wbfsDriveSecondary.EntriesOnDrive; //Set the source for the secondary drive's list box

            DropShadowEffect effect = new DropShadowEffect { Color = Colors.Red, ShadowDepth = 0 };
            FormatButton.Effect = effect;                                       //Set the red glow effect for the format button

            //Set lists to be sorted by name.
            ICollectionView listView = CollectionViewSource.GetDefaultView(wbfsDriveCurrent.EntriesOnDrive);
            ListCollectionView listCollectionView = (ListCollectionView)listView;
            listCollectionView.CustomSort = new NameSorter();
            listView = CollectionViewSource.GetDefaultView(wbfsDriveCurrent.EntriesToAdd);
            listCollectionView = (ListCollectionView)listView;
            listCollectionView.CustomSort = new NameSorter();
            listView = CollectionViewSource.GetDefaultView(wbfsDriveSecondary.EntriesOnDrive);
            listCollectionView = (ListCollectionView)listView;
            listCollectionView.CustomSort = new NameSorter();

            //Parse the language directories to create the language submenu from all the available languages.
            CreateLanguageSubmenu();
        }
        #endregion Constructors

        #region Window Event Handlers
        /// <summary>
        /// Fill list of drives, Set the current UI culture (language), load settings from before
        /// Show Welcome screen if first launch.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GetListOfDrivesOnSystem();      //Get the list of drives on the system

            if (Properties.Settings.Default.FirstRun)       //if this is the first time the users running the app do some initial setting up
            {
                Thread.Sleep(100);         //wait for the whole UI to load in before modally showing the welcome dialog (avoids ugly effect with transparent main window)
                try
                {
                    //if (Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName.Equals("zh"))    //Set all Chinese cultures to zh-China to simplify.
                    //{
                    //    Properties.Settings.Default.Language = "zh-CN";
                    //}
                    //else                                                                                //If it's not a Chinse culture, then get the neutral culture for it by using the two-letter name
                    //{
                    //CultureInfo.GetCultureInfo(Thread.CurrentThread.CurrentUICulture.Parent.Name);     //Set the UI's culture (language) to the neutral version of the user's culture
                    if (Thread.CurrentThread.CurrentUICulture.Name.Equals("it-CH"))     //Since its the first run, if the user's computer is actually Swiss Italian, set it to neutral Italian. (Since we're using Swiss Italian for Perugino)
                        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("it");
                    Properties.Settings.Default.Language = Thread.CurrentThread.CurrentUICulture.Name;      //Save the setting in the settings file
                    try
                    {
                        Infralution.Localization.Wpf.CultureManager.UICulture = new CultureInfo(Properties.Settings.Default.Language);      //Update the UI language before showing the welcome dialog
                    }
                    catch
                    {
                        if (Properties.Settings.Default.Language.Equals("zh-CHT"))
                            Infralution.Localization.Wpf.CultureManager.UICulture = CultureInfo.CreateSpecificCulture("zh-TW");
                        else if (Properties.Settings.Default.Language.Equals("zh-CHS"))
                            Infralution.Localization.Wpf.CultureManager.UICulture = CultureInfo.CreateSpecificCulture("zh-CN");
                    }
                    //Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentUICulture.Parent;
                    //}
                }
                catch (ArgumentException)                           //Setting the Chinese culture can cause problems sometimes, so catch the exception and use the full culture name, causing the application to default to English
                {
                    //Fix for Chinese neutral culture inconsistency.
                    Properties.Settings.Default.Language = Thread.CurrentThread.CurrentUICulture.Name;
                }
                //create and modally show the welcome dialog.
                WelcomeDialog welcomeDialog = new WelcomeDialog();
                welcomeDialog.Owner = this;     //set it's owner to this window so it shows up centered.
                welcomeDialog.ShowDialog();

                OptionsDialog od = new OptionsDialog(true) { Owner = this };
                od.ShowDialog();
                Properties.Settings.Default.UseWebCovers = od.DownloadFromWeb;
                Properties.Settings.Default.TempDirectory = od.TempDirectory;
                Properties.Settings.Default.ShowCovers = od.ShowCovers;
                Properties.Settings.Default.EnableRarExtraction = od.EnableRarExtraction;
                Properties.Settings.Default.AutomaticUpdateChecks = od.AutomaticUpdateChecks;
                Properties.Settings.Default.SourceWadPath = od.SrcWadFile;
                Properties.Settings.Default.CommonKeyPath = od.KeyFile;
                Properties.Settings.Default.LoaderFilePath = od.LoaderFile;
                Properties.Settings.Default.LoaderOriginalTitleId = od.OriginalTitleId;
                Properties.Settings.Default.EnableChannelCreation = od.EnableChannelCreation;
                Properties.Settings.Default.PartitionToUse = (int)od.PartitionToUse;
                if (Properties.Settings.Default.CoverDirs == null)
                    Properties.Settings.Default.CoverDirs = new StringCollection();
                foreach (String item in od.CoverDirs)
                {
                    Properties.Settings.Default.CoverDirs.Add(item);
                }

                //Prompt the user if they want to allow downloading covers from the web
                //MessageBoxResult resultWeb = MessageBox.Show(this, Properties.Resources.DownloadFromWebPromptText, Properties.Resources.DownloadFromWebPromptShortText, MessageBoxButton.YesNo, MessageBoxImage.Question);
                //if (od.DownloadFromWeb== MessageBoxResult.Yes)
                //    Properties.Settings.Default.UseWebCovers = true;        //If they do, the set the setting to true
                //else
                //    Properties.Settings.Default.UseWebCovers = false;       //otherwise set it to false

                Properties.Settings.Default.FirstRun = false;               //Remove the first run flag
                Properties.Settings.Default.Save();                         //Save the settings file to disk
            }
            EditShowCoversMenuItem.IsChecked = Properties.Settings.Default.ShowCovers;      //Load the user's setting for showing covers and toggle the checkbox in the menu accordingly
            wbfsDriveCurrent.TempDirectory = Properties.Settings.Default.TempDirectory;     //Load the user's setting for the temporary directory
            wbfsDriveSecondary.TempDirectory = Properties.Settings.Default.TempDirectory;
            if(!Directory.Exists(Properties.Settings.Default.TempDirectory)
            {
                try
                {
                    Directory.CreateDirectory(Properties.Settings.Default.TempDirectory);
                }
                catch
                {
                    MessageBox.Show(this, Properties.Resources.ErrorUnableToCreateTempDirStr + Properties.Settings.Default.TempDirectory, Properties.Resources.ErrorUnableToCreateDirShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                    ShowOptionsDialog();
                }
            }
            LoadExpanderSettings();
            EditDownloadCoversFromWebMenuItem.IsChecked = Properties.Settings.Default.UseWebCovers;     //Set the Download Covers checkbox in the menu to the correct state
            if (Properties.Settings.Default.Language.Trim().Length != 0)                                //If the language setting does have an actual value and isn't empty, set the WPF localizer's language to the selected language
            {
                try
                {
                    Infralution.Localization.Wpf.CultureManager.UICulture = new CultureInfo(Properties.Settings.Default.Language);
                }
                catch
                {
                    if (Properties.Settings.Default.Language.Equals("zh-CHT"))
                        Infralution.Localization.Wpf.CultureManager.UICulture = CultureInfo.CreateSpecificCulture("zh-TW");
                    else if (Properties.Settings.Default.Language.Equals("zh-CHS"))
                        Infralution.Localization.Wpf.CultureManager.UICulture = CultureInfo.CreateSpecificCulture("zh-CN");
                }
                UpdateLanguageMenuCheckBoxes();     //Update the checkboxes for languages in the Language menu
            }
#if DEBUG
            this.Title += " DEBUG " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
#endif
            if (Properties.Settings.Default.AutomaticUpdateChecks)
            {
                CheckForUpdates(false);
            }
        }

        /// <summary>
        /// Event handler for when the user does a drop operation over the Add To ListBox. Adds the dropped ISO files to the list.
        /// </summary>
        private void AddListBox_Drop(object sender, DragEventArgs e)
        {
            String[] filenames = ((String[])e.Data.GetData(DataFormats.FileDrop));   //Get the list of the files that were dropped

            BackgroundWorker bw = new BackgroundWorker();                                       //Create a background worker to do the parsing and archive extraction
            bw.DoWork += bw_ParseDoWork;                                //hook up event handlers
            bw.ProgressChanged += bw_ParseProgressChanged;
            bw.RunWorkerCompleted += bw_ParseRunWorkerCompleted;
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            ArrayList argList = new ArrayList { filenames, bw, Properties.Settings.Default.EnableRarExtraction };
            bw.RunWorkerAsync(argList);                                                       //Pass the list of files dropped as well as the background worker object to allow progress reporting

            pd = new ProgressDialog(Properties.Resources.ReadingFilesText, false, true, filenames.Length, true, bw);     //Create a progress dialog with overall progress enabled and the total number of items set to the number of files
            pd.Owner = this;                                                                           //Set the owner of the dialog to this window to keep it centered
            pd.ShowDialog();                                                                            //Show the dialog modally to keep the user from doing anything else with the program while parsing
        }

        /// <summary>
        /// Event handler for clicking on the www.wiinewz.com link, launches browser and redirects to site.
        /// </summary>
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://www.wiinewz.com");
        }

        /// <summary>
        /// Event handler for clicking on the donate button, launches browser and redirects to PayPal donation link.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DonateButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(@"https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=J9M77A6YJX9SQ&lc=GB&item_name=WBFS%20Manager&item_number=smallButton&currency_code=CAD&bn=PP%2dDonationsBF%3abtn_donate_SM%2egif%3aNonHosted");
        }

        /// <summary>
        /// Event handler for when the close button is clicked (the title bar X or right click close on taskbar). Safely exits the program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindowInstance_Closed(object sender, EventArgs e)
        {
            Exit();     //Call the exit method to handle saving settings and safely exiting.
        }

        /// <summary>
        /// When the Visit Blog menu item is clicked, redirect to the WBFS Manager blog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VisitBlogMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://wbfsmanager.blogspot.com");
        }

        /// <summary>
        /// When the Visit Download menu item is clicked, redirect to the WBFS Manager download site.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VisitDownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://wbfsmanager.codeplex.com");
        }

        /// <summary>
        /// Expand the window size by the size of the right expander, whenever its expanded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToAddExpander_Expanded(object sender, RoutedEventArgs e)
        {
            this.Width += ToAddCoverViewImage.Width;
        }

        /// <summary>
        /// Decrease the window size by the size of the right expander, whenever its closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToAddExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            this.Width -= ToAddCoverViewImage.Width;
        }

        /// <summary>
        /// Expand the window size by the size of the left expander, whenever its expanded. Also shift window left to give it the
        /// effect of staying in the same position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDriveExpander_Expanded(object sender, RoutedEventArgs e)
        {
            this.Width += OnDriveCoverViewImage.Width;
            this.Left -= OnDriveCoverViewImage.Width;
        }

        /// <summary>
        /// Decrease the window size by the size of the left expander, whenever its closed. Also shift the window back right 
        /// giving the window the effect of staying in the same position.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDriveExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            this.Width -= OnDriveCoverViewImage.Width;
            this.Left += OnDriveCoverViewImage.Width;
        }
        #endregion Event Handlers

        #region Methods
        /// <summary>
        /// Safely exits the program, after saving the most recent settings.
        /// </summary>
        private void Exit()
        {
            if (!Utils.CheckForWindowsXP())     //Windows XP shows different behavior for closing drives if they aren't open so only do a final close on non-XP systems
            {
                try
                {
                    WbfsIntermWrapper.CloseDrive();
                }
                catch (AccessViolationException)
                {
#if DEBUG           
                    //Got an AccessViolation!? Shouldn't happen but if it does show an error if in debug mode
                    MessageBox.Show(this, "An error occured while closing the WBFS drive.", "Error closing drive.", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
                }
            }
            wbfsDriveCurrent.EntriesToAdd.Clear();      //Clear the list of entries to add to allow safe deleting of unRARed temp files.
            try
            {
                String[] tempFiles = Directory.GetFiles(wbfsDriveCurrent.TempDirectory, "*.iso", SearchOption.TopDirectoryOnly); //Delete any temporary iso files from unraring.
                foreach (String item in tempFiles)
                {
                    File.Delete(item);          //Get rid of any temporary iso files created from extracted archives.
                }
            }
            catch (Exception exc)
            {
#if DEBUG
                throw exc;
#endif
            }
            Properties.Settings.Default.LoadedDrive = wbfsDriveCurrent.LoadedDriveLetter;       //Save the last loaded drive letter
            Properties.Settings.Default.SecondaryLoadedDrive = wbfsDriveSecondary.LoadedDriveLetter;       //Save the last loaded secondary drive letter
            Properties.Settings.Default.OnDriveExpanded = OnDriveExpander.IsExpanded;           //Save the user's expander settings
            Properties.Settings.Default.ToAddExpanded = ToAddExpander.IsExpanded;
            Properties.Settings.Default.DriveToDriveExpanded = DriveToDriveExpander.IsExpanded;
            try
            {
                Properties.Settings.Default.Save();                                                 //Write the settings to the user's settings file
                Application.Current.Shutdown();                                                     //Shutdown the application
                Process.GetCurrentProcess().Kill();                              //Kill the process
            }
            catch
            {
            }
        }

        /// <summary>
        /// Populates the drive combobox and secondary drive combo box
        /// </summary>
        private void GetListOfDrivesOnSystem()
        {
            //Get the list of drives on the system
            String[] driveList = Directory.GetLogicalDrives();
            foreach (string item in driveList)
            {
                DriveComboBox.Items.Add(item.Substring(0, item.Length - 1));        //Add them to the combo box
                SecondaryDriveComboBox.Items.Add(item.Substring(0, item.Length - 1));
            }
            if (DriveComboBox.Items.Count <= 0)         //No drives detected, somethings wrong, exit the program
            {
                MessageBox.Show(this, Properties.Resources.UnableToDetectDrivesStr, Properties.Resources.NoDrivesDetectedShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                Exit();
            }
            int index = DriveComboBox.Items.IndexOf(Properties.Settings.Default.LoadedDrive + ":");  //Load the last used drive from settings, strip the semicolon and check if it's in the combobox
            if (index != -1)
                DriveComboBox.SelectedIndex = index;        //if it is in the combo box, set the selected index to that item's index
            else
                DriveComboBox.SelectedIndex = 0;            //otherwise set it to the first item on the list.
            index = DriveComboBox.Items.IndexOf(Properties.Settings.Default.SecondaryLoadedDrive + ":");  //Load the last used drive from settings, strip the semicolon and check if it's in the secondary drive combobox
            if (index != -1)
                SecondaryDriveComboBox.SelectedIndex = index;        //if it is in the combo box, set the selected index to that item's index
            else
                SecondaryDriveComboBox.SelectedIndex = 0;            //otherwise set it to the first item on the list.
        }

        /// <summary>
        /// Updates the items in the drive combo boxes to prevent the same drive being loaded twice.
        /// </summary>
        private void SyncDriveComboBoxes()
        {
            SecondaryDriveComboBox.Items.Clear();                           //Clear the secondary drives combo box, add in all the drives except the one thats loaded by wbfdsDriveCurrent
            //Get the list of drives on the system
            String[] driveList = Directory.GetLogicalDrives();
            foreach (string item in driveList)
            {
                if (!wbfsDriveCurrent.LoadedDriveLetter.Equals(String.Empty) && item.Contains(wbfsDriveCurrent.LoadedDriveLetter))  //if a primary drive is loaded and this item is the same drive letter
                    continue;                                                                                       //, dont add it to the combo box for the secondary drive (to prevent loading the same drive twice)
                SecondaryDriveComboBox.Items.Add(item.Substring(0, item.Length - 1));                               //otherwise add it.
            }
            DriveComboBox.Items.Clear();                                //Clear the main drives combo box, add in all the drives except the one thats loaded by wbfdsDriveSecondary
            //Get the list of drives on the system
            foreach (string item in driveList)
            {
                if (!wbfsDriveSecondary.LoadedDriveLetter.Equals(String.Empty) && item.Contains(wbfsDriveSecondary.LoadedDriveLetter))   //if a secondary drive is loaded and this item is the same drive letter
                    continue;                                                                                       //, dont add it to the combo box for the primary drive (to prevent loading the same drive twice)
                DriveComboBox.Items.Add(item.Substring(0, item.Length - 1));                                        //otherwise add it.
            }
            int index = DriveComboBox.Items.IndexOf(wbfsDriveCurrent.LoadedDriveLetter + ":");  //Load the last used drive from settings, strip the semicolon and check if it's in the combobox
            if (index != -1)
                DriveComboBox.SelectedIndex = index;        //if it is in the combo box, set the selected index to that item's index
            else
                DriveComboBox.SelectedIndex = 0;            //otherwise set it to the first item on the list.
            index = SecondaryDriveComboBox.Items.IndexOf(wbfsDriveSecondary.LoadedDriveLetter + ":");  //Load the last used drive from settings, strip the semicolon and check if it's in the secondary drive combobox
            if (index != -1)
                SecondaryDriveComboBox.SelectedIndex = index;        //if it is in the combo box, set the selected index to that item's index
            else
                SecondaryDriveComboBox.SelectedIndex = 0;            //otherwise set it to the first item on the list.
        }

        /// <summary>
        /// Parse the application directory for language resource files and fill in the Language menu
        /// </summary>
        private void CreateLanguageSubmenu()
        {
            String executablePath = IOPath.GetDirectoryName(Assembly.GetExecutingAssembly().Location);  //Get the directory of the application from the running assembly's location
            string[] resourceFiles = Directory.GetFiles(executablePath, WBFSLANGUAGEDLLNAME, SearchOption.AllDirectories);  //Get all files in the app directory that match the language resource filename
            foreach (String resourceFile in resourceFiles)
            {
                try
                {
                    DirectoryInfo di = new DirectoryInfo(IOPath.GetDirectoryName(resourceFile));        //Use the directory info to get the directory name, which is also the culture name

                    CultureInfo ci = new CultureInfo(di.Name);
                    String nativeName = ci.NativeName;                      //Get the name of the culture in that culture's language (ex. Francais, Italiano, etc).
                    nativeName = Char.ToUpper(nativeName[0]) + nativeName.Substring(1, nativeName.Length - 1); //Get capitalized, native name for the language
                    MenuItem langMenuItem = new MenuItem();             //Create a menu item, set the text to the native name of the language
                    //if(ci.TwoLetterISOLanguageName=="zh")
                    //    langMenuItem.Header = new CultureInfo("zh-Hant").NativeName;
                    //else
                    if (ci.Name.Equals("it-CH"))                                        //Check if it's Perugino (actually Swiss Italian)
                        langMenuItem.Header = "Donca";                                  //If it is, set the name to Donca (due to spoofing Perugino as Swiss Italian)
                    else
                        langMenuItem.Header = nativeName;                               //In all other cases, set the name to the language's native name
                    langMenuItem.IsCheckable = true;                    //allow it to be checked
                    langMenuItem.Command = UICommands.ChangeLanguage;   //Set it to execute the ChangeLanguage UI command
                    if (ci.IsNeutralCulture)                                //If the culture is already a neutral culture, use the neutral culture name as the command param (ex. zh-CHT)
                        langMenuItem.CommandParameter = di.Name;            //Set the command's parameter to the two letter name of the culture (same as directory name)
                    else if (ci.Name.Equals("it-CH"))                        //if it's Perugino, use the specific culture name as the command param (i.e. it-CH)
                        langMenuItem.CommandParameter = ci.Name;
                    else
                        langMenuItem.CommandParameter = ci.Parent.Name;     //Otherwise use the neutral culture as the command param
                    EditLanguageMenuItem.Items.Add(langMenuItem);       //Add the menu item to the Language menu

                }
                catch
                {
#if DEBUG
                    MessageBox.Show(this, "Error occured while parsing language directories: " + resourceFile);
#endif
                }

            }
            EditLanguageMenuItem.Items.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));    //Sort the language menu by the text
        }

        /// <summary>
        /// Loads the user's expander settings, making sure to set the window sizes appropriately
        /// </summary>
        private void LoadExpanderSettings()
        {
            OnDriveExpander.IsExpanded = Properties.Settings.Default.OnDriveExpanded;       //Load the user's last settings for the expanders
            ToAddExpander.IsExpanded = Properties.Settings.Default.ToAddExpanded;
            DriveToDriveExpander.IsExpanded = Properties.Settings.Default.DriveToDriveExpanded;
        }

        /// <summary>
        /// Error handler for passing to libwbfs. Needs to be hooked up by passing it as a delegate (func. pointer) to SubscribeErrorHandler.
        /// Currently left out of the implementation due to issues with the Garbage Collector picking it up and causing an exception when it was finally called.
        /// </summary>
        /// <param name="errorMessage"></param>
        public void WBFSErrorHandler(StringBuilder errorMessage)
        {
            MessageBox.Show(errorMessage.ToString());
        }

        /// <summary>
        /// Asks the WbfsDrive object to load the drive with the given letter.
        /// Shows an error message if it fails. If it succeeds it enables the main portion of the UI.
        /// </summary>
        /// <param name="drive">The drive letter of the drive to be loaded.</param>
        /// <param name="driveObject">The drive object to load into.</param>
        /// <returns>True if successfuly, False if failed.</returns>
        private bool LoadDrive(String drive, WbfsDrive driveObject)
        {
            try
            {
                DriveInfo driveInfo = new DriveInfo(drive);
                if (driveInfo.DriveType == DriveType.CDRom || (driveInfo.DriveType == DriveType.Network && !driveInfo.IsReady))         //If the drive is a CD/DVD drive show an error message or if it's an offline network drive
                {
                    MessageBox.Show(this, Properties.Resources.ErrorLoadingDriveStr, Properties.Resources.ErrorLoadingDriveShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                //if (driveInfo.IsReady && (driveInfo.DriveFormat.Equals("NTFS") || driveInfo.DriveFormat.Contains("FAT") || driveInfo.DriveFormat.Contains("ext")))     //Make sure it's not an NTFS, FAT or ext drive first (if it is, it'll be "Ready" as well)
                //{                                                                                                                               //If it is, show an error message, since these formats cannot be loaded
                //    MessageBox.Show(this, Properties.Resources.ErrorLoadingDriveStr, Properties.Resources.ErrorLoadingDriveShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                //    return false;
                //}
                //Note: (EDIT: Seems not to be the case.) WBFS drives are indicated as IsReady == false and have null values for DriveFormat. We assume the WBFS drive is not a DVD drive or an offline network drive, and that it's not FAT, NTFS or ext
                driveObject.FatalErrorCallback = WBFSErrorHandler;   //Give the error handler to the wbfsDrive object, but since the object doesnt do the subscribe this effectively does nothing
                if (!driveObject.LoadDrive(drive))      //Tell WbfsDrive instance to load the drive
                {
                    MessageBox.Show(this, Properties.Resources.ErrorLoadingDriveStr, Properties.Resources.ErrorLoadingDriveShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                return true;
            }
            catch (Exception exc)                       //Catch any unexpected exceptions and handle it gracefully.
            {
                MessageBox.Show(this, Properties.Resources.ErrorLoadingDriveStr, Properties.Resources.ErrorLoadingDriveShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
#if DEBUG
                MessageBox.Show(this, "An exception occured while attempting to load the drive: " + exc.Message + exc.StackTrace);
#endif
                return false;
            }
        }

        /// <summary>
        /// Ask WbfsDrive instance to load the stats for the drive.
        /// </summary>
        private void LoadDriveInfo()
        {
            if (!wbfsDriveCurrent.LoadDriveInfo())      //if it fails to load the stats, show an error message.
            {
                MessageBox.Show(this, Properties.Resources.ErrorReadingDriveStatsStr, Properties.Resources.ErrorReadingDriveStatsShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            //Otherwise bind the value lables to the value properties of the DriveStats object in WbfsDrive
            Binding bindings = new Binding("TotalSpaceString") { Mode = BindingMode.OneWay, Source = wbfsDriveCurrent.DriveStats };
            TotalSpaceLabel.SetBinding(Label.ContentProperty, bindings);

            bindings = new Binding("UsedSpaceString") { Mode = BindingMode.OneWay, Source = wbfsDriveCurrent.DriveStats };
            UsedSpaceLabel.SetBinding(Label.ContentProperty, bindings);

            bindings = new Binding("FreeSpaceString") { Mode = BindingMode.OneWay, Source = wbfsDriveCurrent.DriveStats };
            FreeSpaceLabel.SetBinding(Label.ContentProperty, bindings);

            bindings = new Binding("UsedBlocksString") { Mode = BindingMode.OneWay, Source = wbfsDriveCurrent.DriveStats };
            BlocksUsedLabel.SetBinding(Label.ContentProperty, bindings);

            NotifyPropertyChanged("PercentDiskSpaceUsed");
            NotifyPropertyChanged("PercentDiskPlusToAdd");
        }

        /// <summary>
        /// Ask the WbfsDriveSecondary instance to load the stats for the drive.
        /// </summary>
        private void LoadSecondaryDriveInfo()
        {
            if (!wbfsDriveSecondary.LoadDriveInfo())      //if it fails to load the stats, show an error message.
            {
                MessageBox.Show(this, Properties.Resources.ErrorReadingDriveStatsStr, Properties.Resources.ErrorReadingDriveStatsShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            //Otherwise bind the value lables to the value properties of the DriveStats object in WbfsDrive
            Binding bindings = new Binding("FreeSpaceString") { Mode = BindingMode.OneWay, Source = wbfsDriveSecondary.DriveStats };
            FreeSpaceOnSecondaryLabel.SetBinding(Label.ContentProperty, bindings);

            bindings = new Binding("TotalSpaceString") { Mode = BindingMode.OneWay, Source = wbfsDriveSecondary.DriveStats };
            TotalSpaceOnSecondaryLabel.SetBinding(Label.ContentProperty, bindings);

            bindings = new Binding("UsedSpaceString") { Mode = BindingMode.OneWay, Source = wbfsDriveSecondary.DriveStats };
            UsedSpaceOnSecondaryLabel.SetBinding(Label.ContentProperty, bindings);
        }

        #region Extraction Code
        /// <summary>
        /// Background worker event handler for when RunWorkerAsync is called to do extraction.
        /// Pass the arguments over to the WbfsDrive instance and have it extract. The background worker is necessary to keep the UI responsive,
        /// by doing the processing and file IO on a separate thread.
        /// </summary>
        /// <param name="e">
        /// The DoWork argument passed to RunWorkerAsync should be an ArrayList with:
        /// 0: the list of selected items to extract (from the EntryListBox)
        /// 1: the filename for the extracted iso if it's one file, or the directory to extract to if it's more than one file
        /// 2: the background worker object, used to allow reporting progress
        /// </param>
        void bwExtract_DoWork(object sender, DoWorkEventArgs e)
        {
            ArrayList argList = (ArrayList)e.Argument;
            e.Result = wbfsDriveCurrent.ExtractDiscFromDrive((IList)argList[1], (String)argList[0], (BackgroundWorker)argList[2]);
            //Call the extraction method and set the list of errors as the result of the whole operation (to be used in the Completed event handler)
        }

        /// <summary>
        /// Event handler for when ExtractDiscFromDrive (and libwbfs as well) provides a progress update.
        /// </summary>
        /// <param name="e">
        /// e.ProgressPercentage should be the current number of items done.
        /// e.UserState should be an ArrayList with two items:
        /// 0: A string with the word "Item" if it's an update on the current item's progress, or "Overall" if it's an update on the overall progress
        /// 1: The total number of operations to be done
        /// </param>
        void bwExtract_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

            ArrayList argList = (ArrayList)e.UserState;       //Get the ArrayList with the extra data from the UserState
            if (((string)argList[0]).Equals("Item"))                                                //If it's an item update do the calcuation (current*100)/total
            {                                                                                       //and tell the progress dialog to update its progress bar.
                pd.SetProgress((e.ProgressPercentage * 100f) / (int)argList[1]);
            }
            else if (((string)argList[0]).Equals("Overall"))                                        //If it's an overlall update do the calculation (current*100)/total
            {                                                                                       //and tell the progress dialog the percentage as well as the distinct numberical values (used to show the current/total text)
                pd.SetProgressOverall(e.ProgressPercentage * 100f / (int)argList[1], e.ProgressPercentage, (int)argList[1]);
            }
            else
            {
#if DEBUG
                throw new Exception("Unknown UserState passed to bwExtract_ProgressChanged.");
#endif
            }
        }

        /// <summary>
        /// Event handler for when the background worker finishes doing the extraction process.
        /// If the number of ErrorDetails is zero then no errors occured. If not, show a generic error message instructing the user to check the tooltips for the red X icons.
        /// </summary>
        /// <param name="e">
        /// e.Result will contain a List<ErrorDetails> which contains any error message needed to be output.
        /// </param>
        void bwExtract_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Tell the progress dialog to disable the Close command override, allowing the dialog to be closed
            pd.ProgrammaticClose = true;
            pd.Close();     //Close the progress dialog
            List<ErrorDetails> errorList = (List<ErrorDetails>)e.Result;            //Retrieve the list of errors from the result of the operation
            if (errorList.Count == 0)                                               //If no errors, then completed successfully.
                MessageBox.Show(this, Properties.Resources.CompletedExtractStr, Properties.Resources.CompletedExtractShortStr, MessageBoxButton.OK, MessageBoxImage.Information);
            else
            {               //Otherwise tell the user to check the tooltips on the games marked with the red X icon.
                MessageBox.Show(this, Properties.Resources.ErrorOccuredExtractingStr, Properties.Resources.ErrorsOccuredExtractingShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region AddToDrive Code
        /// <summary>
        /// Background worker event handler for when RunWorkerAsync is called to do addition of games to WBFS drive.
        /// Pass the arguments over to the WbfsDrive instance and have it add games. The background worker is necessary to keep the UI responsive,
        /// by doing the processing and file IO on a separate thread.
        /// The list of errors that occur are put in the result of the operation for use when the operation has completed.
        /// </summary>
        /// <param name="e">The e.Argument value should be the whole entriesToAdd list.</param>
        void bwAddToDrive_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = wbfsDriveCurrent.AddToDrive((ObservableCollection<WbfsEntry>)e.Argument, (BackgroundWorker)sender, (WbfsIntermWrapper.PartitionSelector)Properties.Settings.Default.PartitionToUse);
        }

        /// <summary>
        /// Event handler for when AddToDrive (and libwbfs as well) provides a progress update.
        /// </summary>
        /// <param name="e">
        /// e.ProgressPercentage should be the current number of items done.
        /// e.UserState should be an ArrayList with two items:
        /// 0: A string with the word "Item" if it's an update on the current item's progress, or "Overall" if it's an update on the overall progress
        /// 1: The total number of operations to be done
        /// </param>
        void bwAddToDrive_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ArrayList argList = (ArrayList)e.UserState;       //Get the ArrayList with the extra data from the UserState
            if (((string)argList[0]).Equals("Item"))                                                //If it's an item update do the calcuation (current*100)/total
            {                                                                                       //and tell the progress dialog to update its progress bar.
                pd.SetProgress((e.ProgressPercentage * 100f) / (int)argList[1]);
            }
            else if (((string)argList[0]).Equals("Overall"))                                        //If it's an overlall update do the calculation (current*100)/total
            {                                                                                       //and tell the progress dialog the percentage as well as the distinct numberical values (used to show the current/total text)
                pd.SetProgressOverall(e.ProgressPercentage * 100f / (int)argList[1], e.ProgressPercentage, (int)argList[1]);
            }
            else
            {
#if DEBUG
                throw new Exception("Unknown UserState passed to bwAddToDrive_ProgressChanged.");
#endif
            }
        }

        /// <summary>
        /// Event handler for when the background worker finishes doing the adding process.
        /// If the number of ErrorDetails is zero then no errors occured. If not, show a generic error message instructing the user to check the tooltips for the red X icons.
        /// Finally, reload the drive info, causing the UI to refresh (since it's bound)
        /// </summary>
        /// <param name="e">
        /// e.Result will contain a List<ErrorDetails> which contains any error message needed to be output.
        /// </param>
        void bwAddToDrive_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Tell the progress dialog to disable the Close command override, allowing the dialog to be closed
            pd.ProgrammaticClose = true;
            pd.Close();             //Close the progress dialog
            List<ErrorDetails> errorList = (List<ErrorDetails>)e.Result;        //Retrieve the list of errors from the result of the operation
            if (errorList.Count == 0)                                           //If no errors, then completed successfully.
                MessageBox.Show(this, Properties.Resources.CompletedAddingStr, Properties.Resources.CompletedAddingShortStr, MessageBoxButton.OK, MessageBoxImage.Information);
            else                                                                //Otherwise tell the user to check the tooltips on the games marked with the red X icon.
                MessageBox.Show(this, Properties.Resources.ErrorsOccuredWhileAddingStr, Properties.Resources.ErrorsOccuredWhileAddingShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
            int result = wbfsDriveCurrent.ReloadDrive();                //Reload the drive in order to update the list of games on the drive as well as the stats
            if (result == -1)                                           //if reloading fails for some reason show the appropriate error messages.
            {
                MessageBox.Show(this, Properties.Resources.ErrorLoadingDriveStr, Properties.Resources.ErrorLoadingDriveShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (result == -2)
            {
                MessageBox.Show(this, Properties.Resources.ErrorReadingDriveStatsStr, Properties.Resources.ErrorReadingDriveStatsShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            NotifyPropertyChanged("PercentDiskSpaceUsed");
            NotifyPropertyChanged("PercentDiskPlusToAdd");
        }
        #endregion

        #region ParseIsoFiles Asnyc code
        /// <summary>
        /// Background worker event handler for when RunWorkerAsync is called to do parsing.
        /// Pass the arguments over to the WbfsDrive instance and have it parse. The background worker is necessary to keep the UI responsive,
        /// by doing the processing and file IO on a separate thread.
        /// </summary>
        /// <param name="e">
        /// The DoWork argument passed to RunWorkerAsync should be an ArrayList with:
        /// 0: the list of filenames to parse (dropped or browsed)
        /// 1: the background worker object, used to allow reporting progress
        /// </param>
        void bw_ParseDoWork(object sender, DoWorkEventArgs e)
        {
            ArrayList argList = (ArrayList)e.Argument;
            e.Result = wbfsDriveCurrent.ParseIsoFileList((String[])argList[0], (BackgroundWorker)argList[1], false, (bool)argList[2], (WbfsIntermWrapper.PartitionSelector)Properties.Settings.Default.PartitionToUse);            //Parse the list of files and add them to the EntriesToAdd list
        }

        /// <summary>
        /// Event handler for when ParseIsoFileList (and UnRar as well) provides a progress update.
        /// </summary>
        /// <param name="e">
        /// e.ProgressPercentage should be the current number of items done. (or the percentage if its an item update).
        /// e.UserState should be an ArrayList with two items:
        /// 0: A string with the word "Item" if it's an update on the current item's progress, or "Overall" if it's an update on the overall progress
        /// 1: The total number of operations to be done (or the percentage done in double form if its an item update).
        /// </param>
        void bw_ParseProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ArrayList argList = (ArrayList)e.UserState;       //Get the ArrayList with the extra data from the UserState
            if (((string)argList[0]).Equals("Item"))                                                //If it's an item update it's already a percentage in this case, pass that along
            {                                                                                       //and tell the progress dialog to update its progress bar.
                pd.SetProgress((double)argList[1]);
            }
            else if (((string)argList[0]).Equals("Overall"))                                        //If it's an overall update do the calculation (current*100)/total
            {                                                                                       //and tell the progress dialog the percentage as well as the distinct numberical values (used to show the current/total text)
                pd.SetProgressOverall(e.ProgressPercentage * 100f / (int)argList[1], e.ProgressPercentage, (int)argList[1]);
            }
            else
            {
#if DEBUG
                throw new Exception("Unknown UserState passed to bwExtract_ProgressChanged.");
#endif
            }
        }

        /// <summary>
        /// Event handler for when the background worker finishes doing the parse process.
        /// If the number of ErrorDetails is zero then no errors occured. If not, show a generic error message instructing the user to check the tooltips for the red X icons.
        /// </summary>
        /// <param name="e">
        /// e.Result will contain an ArraList containing:
        /// 0: List<WbfsEntry> which contains the entries to be added. (Can't do this on a separate thread, so have to do it here)
        /// 1: List<ErrorDetails> which contains any error message needed to be output.
        /// </param>
        void bw_ParseRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Tell the progress dialog to disable the Close command override, allowing the dialog to be closed
            pd.ProgrammaticClose = true;
            pd.Close();     //Close the progress dialog
            ArrayList resultList = (ArrayList)e.Result;
            List<WbfsEntry> entriesToBeAdded = (List<WbfsEntry>)resultList[0];
            foreach (WbfsEntry item in entriesToBeAdded)
            {
                wbfsDriveCurrent.EntriesToAdd.Add(item);
            }
            List<ErrorDetails> errorList = (List<ErrorDetails>)resultList[1];
            if (errorList.Count != 0)                                                               //If there are errors, show a message box with the error messages
            {
                StringBuilder errorMessage = new StringBuilder();
                errorMessage.AppendLine(Properties.Resources.ErrorReadingSomeIsosStr);                //Write general error message
                foreach (ErrorDetails item in errorList)
                {
                    errorMessage.AppendLine(item.LongMessage);                                      //Add file specific messages line-by-line
                }
                MessageBox.Show(this, errorMessage.ToString(), Properties.Resources.ErrorReadingSomeIsosStr, MessageBoxButton.OK, MessageBoxImage.Error); //Show the error message
            }
            NotifyPropertyChanged("EstimatedTotalSizeString");      //Tell the UI to update the Estimated Total Size label
            NotifyPropertyChanged("PercentDiskPlusToAdd");
        }
        #endregion

        #region Create Channel Code
        /// <summary>
        /// Makes the call to CreateChannel on the background worker's separate thread.
        /// Argument passed as e.Argument is an ArrayList with the following:
        /// 0: An IList with the WbfsEntries to be processed.
        /// 1: The output path for the WADs.
        /// 2: The calling BackgroundWorker (to allow progress reporting).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void createChannelWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ArrayList argList = (ArrayList)e.Argument;      //Extract the arguments, make the call, and set the result of the operation to the list of errors returned (used by workerCompleted).
            e.Result = CreateChannel((IList)argList[0], (String)argList[1], String.Empty, (BackgroundWorker)argList[2]);
        }

        /// <summary>
        /// Handles the progress reports made by the BackgroundWorker (on behalf of CreateChannel).
        /// Only overall progress is reported for channel creation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void createChannelWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                //Do the calculations and tell the progress bar to update the status.
                pd.SetProgressOverall(e.ProgressPercentage * 100f / (int)e.UserState, e.ProgressPercentage, (int)e.UserState);
            }
            catch (Exception exc)
            {
#if DEBUG 
                MessageBox.Show(this, "Error updating progress dialog: " + exc.Message);
#endif
                return;
            }
        }

        /// <summary>
        /// Handles the completion event for Channel Creation. Closes the progress dialog, shows any error messages and shows a completion message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void createChannelWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Tell the progress dialog to disable the Close command override, allowing the dialog to be closed
            pd.ProgrammaticClose = true;
            pd.Close();             //Close the progress dialog
            if (e.Result != null)
            {
                List<ErrorDetails> errors = (List<ErrorDetails>)e.Result;
                if (errors.Count != 0)
                {
                    StringBuilder errorString = new StringBuilder();
                    errorString.AppendLine(Properties.Resources.ErrorFollowingWhileTransStr);
                    foreach (ErrorDetails item in errors)
                    {
                        errorString.AppendLine(item.LongMessage);
                    }
                    MessageBox.Show(this, errorString.ToString(), Properties.Resources.ErrorsCreatingChannelsStr, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            MessageBox.Show(this, Properties.Resources.ChannCreationCompleteStr, Properties.Resources.CompletedStr, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Creates channels for each WbfsEntry in the IList passed in the specified output path witht the given channel ID.
        /// </summary>
        /// <param name="entriesToCreateFor">An IList of WbfsEntries to create channels for. (Using IList due to ListBox.SelectedItems not being a generic IList)</param>
        /// <param name="outputPath">If theres only one item in the list, this should be the full filename (including the path) to save the file as. If theres more than one item, this should be a folder.</param>
        /// <param name="channelId">The channel ID to save single items as. (Not used when list has more than one items, can be empty in that case).</param>
        /// <param name="worker">The calling background worker (used to allow progress reporting).</param>
        /// <returns></returns>
        private List<ErrorDetails> CreateChannel(IList entriesToCreateFor, String outputPath, String channelId, BackgroundWorker worker)
        {
            //Create the name of the temp folder (inside the usual WBFS Manager temp folder, so as to avoid any mix ups and allow safe deleting of the folder once the process is done).
            string tempFolder = Path.Combine(Properties.Settings.Default.TempDirectory, "ChanTemp");
            List<ErrorDetails> errorList = new List<ErrorDetails>();            //A list of ErrorDetails to hold any error messages that may occurr.
            if (entriesToCreateFor.Count == 0)                                  //If there are no entries in the list return with an error message.
            {
                errorList.Add(new ErrorDetails(Properties.Resources.ErrorEmptyEntryListStr, Properties.Resources.ErrorEmptyEntryListShortStr));
                return errorList;
            }
            try
            {
                if (Directory.Exists(tempFolder))                               //If the ChanTemp folder exists, delete it and recreate it to avoid any mix ups with leftover (possibly corrupted) files.
                {
                    Directory.Delete(tempFolder, true);
                }
                Directory.CreateDirectory(tempFolder);
                if (entriesToCreateFor.Count == 1)                              //Only one file, so interpret outputPath as a full filename.
                {
                    WbfsEntry entryToCreateFor = (WbfsEntry)entriesToCreateFor[0];          //Grab the first (and only) WbfsEntry off the list.
                    if (!entryToCreateFor.IsFilePathValid())                                //Make sure the file path is valid (only checks to make sure its not :ONDRIVE:, meaning its not an iso on the user's computer)
                    {
                        errorList.Add(new ErrorDetails(Properties.Resources.ErrorInvalidIsoPathStr, Properties.Resources.ErrorInvalidIsoPathShortStr));
                        return errorList;
                    }
                    //Call create channel, passing the path to the ISO (which is kept in the WbfsEntry), the base WAD path, the path to the loader file, it's placeholder id, path to the common key (taken from the user's settings), 
                    // the disc ID of the WbfsEntry (the game), the new (4 letter) channel ID, the temporary folder to use and the output filename.
                    int result = ChannelCreationWrapper.ChannelCreationWrapper.CreateChannel(
                        entryToCreateFor.FilePath,
                        Properties.Settings.Default.SourceWadPath,
                        Properties.Settings.Default.LoaderFilePath,
                        Properties.Settings.Default.LoaderOriginalTitleId,
                        Properties.Settings.Default.CommonKeyPath,
                        entryToCreateFor.EntryID,
                        channelId,
                        tempFolder,
                        outputPath);
                    if (result != 0)                //If the result is not 0, there was an error, so add a generic error message, and return it (since its the only item to process).
                    {
                        errorList.Add(new ErrorDetails(Properties.Resources.ErrorCreatingChannelErrCodeStr + result + ")", Properties.Resources.ErrorCreatinChanSingleStr));
                        return errorList;
                    }
                }
                else                                                            //Multiple files, so generate IDs and filenemes, interpret the outputPath as a directory.
                {
                    int count = 0;
                    foreach (WbfsEntry item in entriesToCreateFor)              //Do the process for each of the items in the list
                    {
                        if (worker.CancellationPending)                                     //if the user requests cancellation, stop as far as we've gotten.
                            break;
                        worker.ReportProgress(count, entriesToCreateFor.Count);
                        count++;

                        string generatedChanId = LEADINGCHANLETTER + item.EntryID.Substring(1, 3);  //Generate the channel ID by replacing the first character of the game's disc ID with the LEADINGCHANLETTER constant, and using the next 3 letters.
                        string subTempFolder = Path.Combine(tempFolder, generatedChanId);           //Genearate a subfolder name in the ChanTemp folder, for this specific game.
                        string itemFilename = item.EntryName;
                        foreach (char invalidChar in Path.GetInvalidFileNameChars())                //Strip out the invalid filename characters from the game's name
                        {
                            itemFilename = itemFilename.Replace(invalidChar, '_');
                        }
                        itemFilename = Path.Combine(outputPath, itemFilename + ".wad");             //Create a filename based on the specified output folder and the sanitized filename.

                        try
                        {
                            if (!item.IsFilePathValid())                                            //Make sure the path to the ISO is a real path.
                            {
                                errorList.Add(new ErrorDetails(Properties.Resources.ErrorInvalidIsoPathForStr + item.EntryName, Properties.Resources.ErrorInvalidIsoPathShortStr));
                                continue;
                            }
                            if (Directory.Exists(subTempFolder))                                    //Create the subfolder generated above, by deleting if it already exists and creating it.
                                Directory.Delete(subTempFolder, true);
                            Directory.CreateDirectory(subTempFolder);

                            //Call create channel, passing the path to the ISO (which is kept in the WbfsEntry), the base WAD path, the path to the loader file, it's placeholder id, path to the common key (taken from the user's settings), 
                            // the disc ID of the WbfsEntry (the game), the new (4 letter) channel ID, the temporary folder to use and the output filename.
                            int result = ChannelCreationWrapper.ChannelCreationWrapper.CreateChannel(
                                item.FilePath,
                                Properties.Settings.Default.SourceWadPath,
                                Properties.Settings.Default.LoaderFilePath,
                                Properties.Settings.Default.LoaderOriginalTitleId,
                                Properties.Settings.Default.CommonKeyPath,
                                item.EntryID,
                                generatedChanId,
                                subTempFolder,
                                itemFilename);
                            if (result != 0)                                   //If the result is not 0, there was an error, so add a generic error message, and return it (since its the only item to process).
                            {
                                errorList.Add(new ErrorDetails(Properties.Resources.ErrorCreatingChanForStr + item.EntryName + Properties.Resources.ErrorCodeStr + result + ")", Properties.Resources.ErrorCreatinChanSingleStr));
                                continue;
                            }
                        }
                        catch (Exception e)                                     //If an exception occurs, call ChannelCreationExceptionHandler to output an error message and save a log then move on to the next item in the list.
                        {                                                       //Catch it here to avoid the program crashing completely
                            ChannelCreationExceptionHandler(itemFilename, generatedChanId, subTempFolder, errorList, e);
                            continue;
                        }

                    }
                }

                if (Directory.Exists(tempFolder))                               //Delete the ChanTemp folder recursively.
                {
                    Directory.Delete(tempFolder, true);
                }
            }
            catch (Exception e)                                                 //If an exception occurs, call ChannelCreationExceptionHandler to output an error message and save a log.
            {
                ChannelCreationExceptionHandler(outputPath, channelId, tempFolder, errorList, e);
            }
            return errorList;                                                   //return the list of errors
        }

        /// <summary>
        /// Show an error message and save an error message to the log. (Doing it specifically for Channel creation since it's less tested and somewhat messy.
        /// </summary>
        /// <param name="outputFilename"></param>
        /// <param name="channelId"></param>
        /// <param name="outfolder"></param>
        /// <param name="errorList"></param>
        /// <param name="e"></param>
        private static void ChannelCreationExceptionHandler(String outputFilename, String channelId, string outfolder, List<ErrorDetails> errorList, Exception e)
        {
            try
            {
                StreamWriter sw = new StreamWriter(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "log.txt"), true);
                if (e.InnerException != null)
                {
                    sw.WriteLine("An unexpected channel creation error occurred. Please send the following information to the application developer at wbfsmanager@gmail.com (You can copy this text by pressing Ctrl+C or use the log file in the installation directory (must Run as Administrator in Vista+)): " +
                  "Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + ", OS: " + Environment.OSVersion.VersionString + " Message: " + e.Message + " \nInnerException: " + e.InnerException.Message
                      + " \nStack trace: " + e.StackTrace + "\nDetails: " + Properties.Settings.Default.SourceWadPath + " " + Properties.Settings.Default.LoaderFilePath + " " + Properties.Settings.Default.LoaderOriginalTitleId + " " + Properties.Settings.Default.CommonKeyPath + " " + channelId + " " + outfolder + " " + outputFilename);
                }
                else
                {
                    sw.WriteLine("An unexpected channel creation error occurred. Please send the following information to the application developer at wbfsmanager@gmail.com (You can copy this text by pressing Ctrl+C or use the log file in the installation directory (must Run as Administrator in Vista+)): " +
                  "Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + ", OS: " + Environment.OSVersion.VersionString + " Message: " + e.Message
                      + " \nStack trace: " + e.StackTrace + "\nDetails: " + Properties.Settings.Default.SourceWadPath + " " + Properties.Settings.Default.LoaderFilePath + " " + Properties.Settings.Default.LoaderOriginalTitleId + " " + Properties.Settings.Default.CommonKeyPath + " " + channelId + " " + outfolder + " " + outputFilename);
                }
                sw.Flush();
                sw.Close();
            }
            catch
            {
            }
            if (e.GetType() == typeof(BadImageFormatException))
            {
                errorList.Add(new ErrorDetails(Properties.Resources.ErrorIncorrectVersionStr, Properties.Resources.ErrorIncorrectVersionShortStr));
            }
            else if (e.InnerException != null)
            {
                errorList.Add(new ErrorDetails("An unexpected channel creation error occurred. Please send the following information to the application developer at wbfsmanager@gmail.com (You can copy this text by pressing Ctrl+C or use the log file in the installation directory (must Run as Administrator in Vista+)): " +
                  "Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + ", OS: " + Environment.OSVersion.VersionString + " Message: " + e.Message + " \nInnerException: " + e.InnerException.Message
                      + " \nStack trace: " + e.StackTrace + "\nDetails: " + Properties.Settings.Default.SourceWadPath + " " + Properties.Settings.Default.LoaderFilePath + " " + Properties.Settings.Default.LoaderOriginalTitleId + " " + Properties.Settings.Default.CommonKeyPath + " " + channelId + " " + outfolder + " " + outputFilename, "Unexpected Error"));
            }
            else
            {
                errorList.Add(new ErrorDetails("An unexpected channel creation error occurred. Please send the following information to the application developer at wbfsmanager@gmail.com (You can copy this text by pressing Ctrl+C or use the log file in the installation directory (must Run as Administrator in Vista+)): " +
                  "Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + ", OS: " + Environment.OSVersion.VersionString + " Message: " + e.Message
                      + " \nStack trace: " + e.StackTrace + "\nDetails: " + Properties.Settings.Default.SourceWadPath + " " + Properties.Settings.Default.LoaderFilePath + " " + Properties.Settings.Default.LoaderOriginalTitleId + " " + Properties.Settings.Default.CommonKeyPath + " " + channelId + " " + outfolder + " " + outputFilename, "Unexpected Error"));
            }
        }

        #endregion

        /// <summary>
        /// Asks WbfsDrive instance to create Homebrew Channel entries from a single Wbfs entry.
        /// </summary>
        /// <param name="entry">The entry to make the HBC entry for.</param>
        /// <param name="outputFolder">The folder to put the games folder in. Creates a folder with the disc ID of the game in this folder.</param>
        /// <returns></returns>
        private bool CreateHbcEntry(WbfsEntry entry, String outputFolder)
        {
            String executablePath = IOPath.GetDirectoryName(Assembly.GetExecutingAssembly().Location);      //Get the path to the executable
            String hbcDolFullPath = IOPath.Combine(executablePath, HBCDOLPATH);                             //Use the path to the executable to get the boot.dol and icon.png files
            String hbcPngFullPath = IOPath.Combine(executablePath, HBCPNGPATH);
            if (!wbfsDriveCurrent.CreateHbcEntry(entry, outputFolder, hbcDolFullPath, hbcPngFullPath, HBCORIGTITLEID))      //Pass the path to these files as well as the output folder and entry to WbfsDrive instance to do creation
                return false;                                                                               //If it fails return false
            return true;
        }

        /// <summary>
        /// Updates the checkbox values for the Language menu. Checks the current language and unchecks all others.
        /// </summary>
        private void UpdateLanguageMenuCheckBoxes()
        {
            if (checkedLanguageMenuItem != null)        //If the reference to the currently checked language is not null, uncheck that language.
                checkedLanguageMenuItem.IsChecked = false;
            MenuItem fallbackMenuItem = null;
            CultureInfo ci = new CultureInfo(Properties.Settings.Default.Language);
            foreach (MenuItem item in EditLanguageMenuItem.Items)       //Find the language item that matches the current language
            {
                if (ci.Name.Equals("it-CH") && item.CommandParameter.ToString().Equals(ci.Name))     //Perugino (actually Swiss Italian)
                {
                    checkedLanguageMenuItem = item;                                     //In the case of Perugino, since its a specific culture, the culture's name, not it's neutral culture will be the command param
                    break;                                                              //for the menu item. So if the current menu item is the selected culture's specific name, then it should be checked
                }
                if (ci.IsNeutralCulture && item.CommandParameter.ToString().Equals(ci.Name))
                {
                    checkedLanguageMenuItem = item;                                             //if it's a neutral culture and it's name matches the current menu item found, set the reference to that item and break out
                    break;
                }
                else if (!ci.IsNeutralCulture && item.CommandParameter.ToString().Equals(ci.Parent.Name))
                {
                    checkedLanguageMenuItem = item;                                             //if it's a non-neutral culture, use it's neutral name and see if it matches the current menuitem's command param,
                    break;                                                                      //if so set the reference to that item and break out
                }
                if (item.CommandParameter.ToString().Equals("en"))                               //if the current item is English (the fallback culture), keep a refernce to it, in case the currently active culture is not on the menu 
                    fallbackMenuItem = item;                                                    //(meaning no language support for the active culture, which makes it fallback to English).
            }
            if (checkedLanguageMenuItem == null && fallbackMenuItem != null)                    //The current language wasn't found in the list. Set the checkbox to the default language.
                checkedLanguageMenuItem = fallbackMenuItem;
            if (checkedLanguageMenuItem != null)                                                //if the reference to the currently checked item is not null set the check mark
                checkedLanguageMenuItem.IsChecked = true;                                       //may be null if it was initially null and the current language was not found on the Language menu
        }

        /// <summary>
        /// Rename a disc thats already on the WBFS drive. Shows the rename dialog and asks WbfsDrive instance to set the name of the game to the user's new name.
        /// </summary>
        /// <param name="tempEntry">The entry to set the name of.</param>
        private void RenameDiscOnDrive(WbfsEntry tempEntry)
        {
            RenameDialog rd = new RenameDialog(tempEntry.EntryName);        //Create a rename dialog with the current name set to this entry's name
            rd.Owner = this;                                                //set the owner to this window to keep it centered.
            if (!rd.ShowDialog().GetValueOrDefault(false))                  //Show it modally and check if the user hit anything but ok (cancel, which is false), default to false.
                return;                                                     //if the user cancelled out, then stop.
            if (rd.NewNameValue.Trim().Length == 0 || rd.NewNameValue.Trim().Length > 57)       //Check the length of the user's input to make sure it legal, show an erro if it's not
            {
                MessageBox.Show(this, Properties.Resources.ErrorRenameInvalidStr, Properties.Resources.ErrorRenameInvalidShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            ErrorDetails result = wbfsDriveCurrent.RenameDiscOnDrive(tempEntry, rd.NewNameValue);       //Request a rename of the disc with the new name selected by the user. Returns an ErrorDetails item if an error occured.
            if (result != null)                                                                         //if its not null an error did occur, so show a message box with the error details.
                MessageBox.Show(this, result.LongMessage, result.ShortMessage, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Exports the lsit of games in the EntriesOnDrive ListBox to a comma-separated values file (.csv).
        /// The data is exported in the following order: Game Name, Disc ID, Region, Size on WBFS.
        /// </summary>
        /// <param name="list">The list of games to export the details of. Can be entire list or just selected items.</param>
        /// <param name="fileName">The name of the file to export to.</param>
        private void ExportToListFile(IList list, string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Append);      //Open the file in append mode
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine("Game Name, Disc ID, Region, Size on WBFS (GB)");       //Write the header line
            foreach (WbfsEntry item in list)
            {
                sw.WriteLine((item.EntryName.Contains(",") ? ("\"" + item.EntryName + "\"") : item.EntryName)
                    + ", " + (item.EntryID.Contains(",") ? ("\"" + item.EntryID + "\"") : item.EntryID)
                    + ", " + (item.RegionCodeString.Contains(",") ? ("\"" + item.RegionCodeString + "\"") : item.RegionCodeString)
                    + ", " + (item.EntrySize.ToString("N2").Contains(",") ? ("\"" + item.EntrySize.ToString("N2") + "\"") : item.EntrySize.ToString("N2"))
                    );
                //Write an entry in the file for each game putting any strings that have a comma in them inside quotes
            }
            sw.Flush();     //Flush the stream and close it.
            sw.Close();
            MessageBox.Show(this, Properties.Resources.CompletedExportingListStr, Properties.Resources.CompletedStr, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Show the options dialog and update the Settings file accordingly
        /// </summary>
        private void ShowOptionsDialog()
        {
            if (Properties.Settings.Default.CoverDirs == null)
                Properties.Settings.Default.CoverDirs = new StringCollection();
            //Create the options dialog, allowing cancellation by the user.
            OptionsDialog od = new OptionsDialog(false, Properties.Settings.Default.ShowCovers, Properties.Settings.Default.UseWebCovers, wbfsDriveCurrent.TempDirectory, Properties.Settings.Default.CoverDirs, Properties.Settings.Default.EnableRarExtraction, Properties.Settings.Default.AutomaticUpdateChecks, Properties.Settings.Default.SourceWadPath, Properties.Settings.Default.CommonKeyPath, Properties.Settings.Default.LoaderFilePath, Properties.Settings.Default.LoaderOriginalTitleId, Properties.Settings.Default.EnableChannelCreation, (WbfsIntermWrapper.PartitionSelector)Properties.Settings.Default.PartitionToUse) { Owner = this };
            if (!od.ShowDialog().GetValueOrDefault(false))    //Show the options dialog
                return;
            Properties.Settings.Default.UseWebCovers = od.DownloadFromWeb;      //Update the Settings according to the user's choices.
            Properties.Settings.Default.TempDirectory = od.TempDirectory;
            Properties.Settings.Default.ShowCovers = od.ShowCovers;
            Properties.Settings.Default.EnableRarExtraction = od.EnableRarExtraction;
            Properties.Settings.Default.AutomaticUpdateChecks = od.AutomaticUpdateChecks;
            Properties.Settings.Default.SourceWadPath = od.SrcWadFile;
            Properties.Settings.Default.CommonKeyPath = od.KeyFile;
            Properties.Settings.Default.LoaderFilePath = od.LoaderFile;
            Properties.Settings.Default.LoaderOriginalTitleId = od.OriginalTitleId;
            Properties.Settings.Default.EnableChannelCreation = od.EnableChannelCreation;
            Properties.Settings.Default.PartitionToUse = (int)od.PartitionToUse;
            Properties.Settings.Default.CoverDirs.Clear();                      //Clear the cover dir list first, then add all the items the user selected.
            foreach (String item in od.CoverDirs)
            {
                Properties.Settings.Default.CoverDirs.Add(item);
            }
            Properties.Settings.Default.Save();                                 //Save the user's setting to disk.
            EditShowCoversMenuItem.IsChecked = Properties.Settings.Default.ShowCovers;      //Load the user's setting for showing covers and toggle the checkbox in the menu accordingly
            wbfsDriveCurrent.TempDirectory = Properties.Settings.Default.TempDirectory;     //Load the user's setting for the temporary directory
            EditDownloadCoversFromWebMenuItem.IsChecked = Properties.Settings.Default.UseWebCovers;     //Set the Download Covers checkbox in the menu to the correct state
            if (wbfsDriveCurrent.EntriesToAdd.Count > 0 || wbfsDriveCurrent.EntriesOnDrive.Count > 0)       //If there are any entries in either of the lists, update the cover images with any new images that may be available.
            {
                wbfsDriveCurrent.UpdateCoverImages();
            }
        }

        /// <summary>
        /// Creates a background worker to do the drive-to-drive copying process and also creates a progress dialog and shows it.
        /// </summary>
        /// <param name="entriesToCopy">The list of entries to copy to the target drive.</param>
        /// <param name="temporaryFolder">The temporary folder to use while extracting.</param>
        private void DriveToDriveCopy(IList<WbfsEntry> entriesToCopy, String temporaryFolder)
        {
            BackgroundWorker bw = new BackgroundWorker();                               //Create a background worker and hook up the events (callbacks)
            bw.DoWork += new DoWorkEventHandler(bwDriveToDrive_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwDriveToDrive_RunWorkerCompleted);
            bw.ProgressChanged += new ProgressChangedEventHandler(bwDriveToDrive_ProgressChanged);
            bw.WorkerReportsProgress = true;                                            //Worker should report progress
            bw.WorkerSupportsCancellation = true;                                      //Cancellation is not allowed (no way of cancelling in libwbfs for now)
            ArrayList argList = new ArrayList();  //Create the list of arguments needed for the extraction processs bwExtract_DoWork
            argList.Add(temporaryFolder);                     //First index, the path to the temp folder selected by the user
            argList.Add(entriesToCopy);                                    //second index, the list of files selected by the user (only one in this case, but send the selectedItems and let the extraction method handle that)
            argList.Add(bw);                                                            //third index, the background worker object to allow progress reporting
            bw.RunWorkerAsync(argList);                                                 //Asnychronously call bwExtract_DoWork and pass argument list as the e.Arguments.

            pd = new ProgressDialog(Properties.Resources.DriveToDriveProgressStr, false, true, entriesToCopy.Count, true, bw);       //Create a progress dialog in single item mode, with no overall progress 
            pd.Owner = this;                                                                            //Set the dialog's owner to this window to keep it centered.
            pd.ShowDialog();                                                                            //Show it modally
        }

        /// <summary>
        /// Background worker event handler for when RunWorkerAsync is called to do Drive-To-Drive copying.
        /// Extract the entries from the source drive and add them to the target drive one by one. The background worker is necessary to keep the UI responsive,
        /// by doing the processing and file IO on a separate thread.
        /// </summary>
        /// <param name="e">
        /// The DoWork argument passed to RunWorkerAsync should be an ArrayList with:
        /// 0: the temporary folder to use.
        /// 1: the list of entries to copy
        /// 2: the background worker object, used to allow reporting progress
        /// </param>
        private void bwDriveToDrive_DoWork(object sender, DoWorkEventArgs e)
        {
            ArrayList argList = (ArrayList)e.Argument;    //Unpack the argument list
            IList<WbfsEntry> overallList = (IList<WbfsEntry>)argList[1];                        //the list of entries to process
            List<WbfsEntry> singularList = new List<WbfsEntry>();                               //necessary for the extraction and addition processes (since they take lists)
            String tempFolder = (String)argList[0];                                             //the temp folder to use for extraction
            List<ErrorDetails> errorList = new List<ErrorDetails>();                            //list of errors generated by any of the procedures.
            BackgroundWorker bw = (BackgroundWorker)argList[2];                                 //the background worker, used to report progress
            int count = 0;                                                                      //the number of entries done so far
            ArrayList progressArgList = new ArrayList();                                        //the argument list for overall progress (always the same throughout this process)
            progressArgList.Add("OverallTransfer");
            progressArgList.Add(overallList.Count);

            foreach (WbfsEntry item in overallList)
            {
                if (bw.CancellationPending)                                                     //if the user has requested cancellation, stop as far as we've gotten
                    break;
                bool alreadyOnDrive = false;
                foreach (WbfsEntry itemOnDrive in wbfsDriveSecondary.EntriesOnDrive)
                {
                    if (itemOnDrive.EntryID.Equals(item.EntryID))                    //Check if it's already on the drive,
                    {
                        alreadyOnDrive = true;
                        break;
                    }
                }
                if (alreadyOnDrive)                 //if its already on the drive skip it.
                {
                    item.CopiedState = WbfsEntry.CopiedStates.Failed;           //set its status to failed and add the error message tooltip as well as adding the error to the list of errors
                    errorList.Add(new ErrorDetails(Properties.Resources.ErrorWhileAddingStr + " " + item.EntryName + Properties.Resources.ErrorAddExistsStr, Properties.Resources.ErrorWhileAddingShortStr));
                    item.ErrorMessageToolTip = Properties.Resources.ErrorWhileAddingStr + " " + item.EntryName + Properties.Resources.ErrorAddExistsStr;
                    count++;                                                    //Increment the overall number of items processed
                    continue;                                                   //Move on to the next item
                }
                if (wbfsDriveSecondary.DriveStats.FreeSpace < item.EntrySize)       //Check to see if theres enough space on the target drive for the item.
                {                                                                   // TODO: Fix this. This won't be 100% correct, since the drivestats are not refreshed each time an item is added.
                    item.CopiedState = WbfsEntry.CopiedStates.Failed;               //set its status to failed, set the error message tooltip and add the error to the list of errors.
                    errorList.Add(new ErrorDetails(Properties.Resources.ErrorWhileAddingStr + " " + item.EntryName + Properties.Resources.ErrorNotEnoughSpaceLowerStr, Properties.Resources.ErrorWhileAddingShortStr));
                    item.ErrorMessageToolTip = Properties.Resources.ErrorWhileAddingStr + " " + item.EntryName + Properties.Resources.ErrorNotEnoughSpaceLowerStr;
                    count++;                                                        //Increment the overall number of items processed
                    continue;                                                       //Move on to the next item
                }
                try
                {
                    bw.ReportProgress(count, progressArgList);                      //Update the progress each round (put here to avoid having to do it everytime a continue is used)
                    singularList.Clear();
                    singularList.Add(item);                                         //Clear the singluarList and add the current item. We want to extract one by one, so we're only adding this one item.
                    try
                    {
                        WbfsIntermWrapper.ProgressCallback pc = new WbfsIntermWrapper.ProgressCallback(delegate(int val, int total)
                        {                                                           //Anonymous function to handle progress updates from libwbfs
                            ArrayList directArgList = new ArrayList();      //create a list of argumetns to pass to the background worker's ReportProgress callback
                            directArgList.Add("Item");                                    //Indicate that it's a progress update on the current item
                            directArgList.Add(total);                                     //pass the total number of elements to be processed.
                            bw.ReportProgress(val, directArgList);                    //Call the callback, passing the current value (passed by the libwbfs callback) and the argument list
                        });
                        StringBuilder sourceDriveLetterSb = new StringBuilder(wbfsDriveCurrent.LoadedDriveLetter);
                        StringBuilder secondDriveLetterSb = new StringBuilder(wbfsDriveSecondary.LoadedDriveLetter);
                        if (WbfsIntermWrapper.OpenDrive(sourceDriveLetterSb.ToString()))
                        {
                            if (WbfsIntermWrapper.CanDoDirectDriveToDrive(secondDriveLetterSb) == 0)
                            {
                                StringBuilder discIdSb = new StringBuilder(item.EntryID);
                                int resultDirect = WbfsIntermWrapper.DriveToDriveSingleCopy(secondDriveLetterSb, pc, discIdSb);
                                WbfsIntermWrapper.CloseDrive();                                             //Make sure to close the drive right away to avoid any leftover handles in case the program crashes.
                                if (resultDirect == 0)      //Success
                                {
                                    item.CopiedState = WbfsEntry.CopiedStates.Succeeded;
                                    count++;                //increment number of processed items and move on.
                                    WbfsIntermWrapper.CloseDrive();
                                    continue;
                                }
                                else if (resultDirect == -4)
                                {
                                    item.CopiedState = WbfsEntry.CopiedStates.Failed;
                                    errorList.Add(new ErrorDetails(Properties.Resources.ErrorExtractingP1Str + " " + item.EntryName + Properties.Resources.ErrorExtrNoGameStr, Properties.Resources.ErrorExtractingP1ShortStr));
                                    item.ErrorMessageToolTip = Properties.Resources.ErrorExtractingP1Str + " " + item.EntryName + Properties.Resources.ErrorExtrNoGameStr;
                                    count++;
                                    WbfsIntermWrapper.CloseDrive();
                                    continue;
                                }
                                else if (resultDirect == -2)
                                {
                                    item.CopiedState = WbfsEntry.CopiedStates.Failed;
                                    errorList.Add(new ErrorDetails(Properties.Resources.ErrorExtractingP1Str + " " + item.EntryName + Properties.Resources.ErrorExtrNoDriveStr, Properties.Resources.ErrorExtractingP1ShortStr));
                                    item.ErrorMessageToolTip = Properties.Resources.ErrorExtractingP1Str + " " + item.EntryName + Properties.Resources.ErrorExtrNoDriveStr;
                                    count++;
                                    WbfsIntermWrapper.CloseDrive();
                                    continue;
                                }
                                else if (resultDirect == -3)
                                {
                                    item.CopiedState = WbfsEntry.CopiedStates.Failed;
                                    errorList.Add(new ErrorDetails(Properties.Resources.ErrorWhileAddingStr + " " + item.EntryName + Properties.Resources.ErrorAddExistsStr, Properties.Resources.ErrorWhileAddingShortStr));
                                    item.ErrorMessageToolTip = Properties.Resources.ErrorWhileAddingStr + " " + item.EntryName + Properties.Resources.ErrorAddExistsStr;
                                    count++;
                                    WbfsIntermWrapper.CloseDrive();
                                    continue;
                                }
                                else if (resultDirect == -1)
                                {
                                    item.CopiedState = WbfsEntry.CopiedStates.Failed;
                                    errorList.Add(new ErrorDetails(Properties.Resources.ErrorWhileAddingStr + " " + item.EntryName + ": " + Properties.Resources.ErrorAddingNoDriveStr, Properties.Resources.ErrorAddingNoDriveShortStr));
                                    item.ErrorMessageToolTip = Properties.Resources.ErrorWhileAddingStr + " " + item.EntryName + ": " + Properties.Resources.ErrorAddingNoDriveStr;
                                    count++;
                                    WbfsIntermWrapper.CloseDrive();
                                    continue;
                                }
                                else if (resultDirect == -103)
                                {
                                    item.CopiedState = WbfsEntry.CopiedStates.Failed;
                                    errorList.Add(new ErrorDetails(Properties.Resources.ErrorWhileAddingStr + " " + item.EntryName + Properties.Resources.ErrorNotEnoughSpaceLowerStr, Properties.Resources.ErrorWhileAddingShortStr));
                                    item.ErrorMessageToolTip = Properties.Resources.ErrorWhileAddingStr + " " + item.EntryName + Properties.Resources.ErrorNotEnoughSpaceLowerStr;
                                    count++;
                                    WbfsIntermWrapper.CloseDrive();
                                    continue;
                                }
                            }
                            WbfsIntermWrapper.CloseDrive();
                        }
                    }
                    catch (Exception dExc)
                    {
                        dExc.ToString();
                        try
                        {
                            WbfsIntermWrapper.CloseDrive();
                        }
                        catch
                        {
                        }
                    }
                    String randomFilename = IOPath.Combine(tempFolder, IOPath.GetRandomFileName());     //Generate a random filename to use in the temp folder for extracting to
                    //Call the extraction method and add the list of errors to the list of errors of the whole operation (to be used in the Completed event handler)
                    List<ErrorDetails> result = wbfsDriveCurrent.ExtractDiscFromDrive(singularList, randomFilename, bw);
                    if (result != null && result.Count != 0)                                                //If an error occured, add the error to the list and move on to the next item, after incrementing the number of items processed.
                    {
                        errorList.AddRange(result);                                                         //No need to add the error message tooltip, since that's already done by the extraction process.
                        count++;
                        continue;
                    }
                    item.FilePath = randomFilename;                                                         //Set the location of the entry on drive so that the AddToDrive method can find it and add it.
                    errorList.AddRange(wbfsDriveSecondary.AddToDrive(singularList, bw, (WbfsIntermWrapper.PartitionSelector)Properties.Settings.Default.PartitionToUse));                    //Call the AddToDrive method on the target drive to have it add the entry.
                    try
                    {
                        File.Delete(item.FilePath);                                                         //once its done with the current entry, try to delete it from the temp folder
                    }
                    catch
                    {                                                                                       //may fail due to acces restrictions. if so unset the path to the entry, increment the number of items processed and move on.
                        item.FilePath = String.Empty;
                        count++;
                        continue;
                    }
                    item.FilePath = String.Empty;                                                           //Unset the path since it's now been copied and the file no longer exists in the temp folder (successfully deleted).
                }
                catch (Exception exc)
                {                                                                                           //overarching try catch, in case something unexpected happens.
                    errorList.Add(new ErrorDetails(Properties.Resources.ErrorOccuredWhileTransStr + item.EntryName, Properties.Resources.ErrorOccuredWhileTransShortStr));
#if DEBUG
                    throw exc;
#endif
                    count++;                //Add the error to the list, increment number of processed items and move on.
                    continue;
                }
                count++;                    //Everything was successfuly, so increment the count and let the foreach loop continue.
            }
            e.Result = errorList;           //Once all the entries are done, set the result of the whole operation as the list of errors, to be used by workerComplted event handler.
        }

        /// <summary>
        /// Event handler for when DriveToDrive (and libwbfs as well) provides a progress update.
        /// </summary>
        /// <param name="e">
        /// e.ProgressPercentage should be the current number of items done.
        /// e.UserState should be an ArrayList with two items:
        /// 0: A string with the word "Item" if it's an update on the current item's progress, or "Overall" if it's an update on the overall progress (which is ignored during DriveToDrive copies)
        ///     or "OverallTransfer" if its an update from the DriveToDrive procedure
        /// 1: The total number of operations to be done
        /// </param>
        private void bwDriveToDrive_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ArrayList argList = (ArrayList)e.UserState;       //Get the ArrayList with the extra data from the UserState
            if (((string)argList[0]).Equals("Item"))                                                //If it's an item update do the calcuation (current*100)/total
            {                                                                                       //and tell the progress dialog to update its progress bar.
                pd.SetProgress((e.ProgressPercentage * 100f) / (int)argList[1]);
            }
            else if (((string)argList[0]).Equals("Overall"))                                        //If it's an overlall update from extracting or adding ignore it
            {
                return;
            }
            else if (((string)argList[0]).Equals("OverallTransfer"))                                //If it's an overlall update from Drive-To-Drive transferring do the calculation (current*100)/total
            {                                                                                       //and tell the progress dialog the percentage as well as the distinct numberical values (used to show the current/total text)
                pd.SetProgressOverall(e.ProgressPercentage * 100f / (int)argList[1], e.ProgressPercentage, (int)argList[1]);
            }
            else
            {
#if DEBUG
                throw new Exception("Unknown UserState passed to bwAddToDrive_ProgressChanged.");
#endif
            }
        }

        /// <summary>
        /// Event handler for when the background worker finishes doing the drive-to-drive copy process.
        /// If the number of ErrorDetails is zero then no errors occured. If not, show a generic error message instructing the user to check the tooltips for the red X icons.
        /// </summary>
        /// <param name="e">
        /// e.Result will contain a List<ErrorDetails> which contains any error message needed to be output.
        /// </param>
        private void bwDriveToDrive_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Tell the progress dialog to disable the Close command override, allowing the dialog to be closed
            pd.ProgrammaticClose = true;
            pd.Close();             //Close the progress dialog
            if (e.Result != null)
            {
                List<ErrorDetails> errors = (List<ErrorDetails>)e.Result;
                if (errors.Count != 0)
                {
                    StringBuilder errorString = new StringBuilder();
                    errorString.AppendLine(Properties.Resources.ErrorFollowingWhileTransStr);
                    foreach (ErrorDetails item in errors)
                    {
                        errorString.AppendLine(item.LongMessage);
                    }
                    MessageBox.Show(this, errorString.ToString(), Properties.Resources.ErrorsTransShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            int result = wbfsDriveSecondary.ReloadDrive();                //Reload the drive in order to update the list of games on the drive as well as the stats
            if (result == -1)                                           //if reloading fails for some reason show the appropriate error messages.
            {
                MessageBox.Show(this, Properties.Resources.ErrorLoadingDriveStr, Properties.Resources.ErrorLoadingDriveShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (result == -2)
            {
                MessageBox.Show(this, Properties.Resources.ErrorReadingDriveStatsStr, Properties.Resources.ErrorReadingDriveStatsShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #region Check For Updates code
        /// <summary>
        /// Starts a background process that checks the website to see if there's been an update to the program.
        /// </summary>
        private void CheckForUpdates(bool showCompleted)
        {
            //Process.Start("http://wbfsmanager.blogspot.com");
            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);
            backgroundWorker.RunWorkerAsync(showCompleted);
        }

        /// <summary>
        /// Call Utils.CheckForNewerVersion. If the result is not-null, theres an update available so set that as the result of the background operation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ArrayList resultList = new ArrayList();
            String result = String.Empty;
            try
            {
                result = Utils.CheckForNewerVersion();
            }
            catch
            {
                resultList = null;
                e.Result = null;
                return;
            }

            if (result.Equals(String.Empty))
            {
                resultList.Add(e.Argument);
                e.Result = resultList;
                return;
            }
            resultList.Add(e.Argument);
            resultList.Add(result);
            e.Result = resultList;
        }

        /// <summary>
        /// Check the result of the operation, if it's null, there no new updates. Otherwise cast the result to a string and parse the XAML,
        /// which is a FlowDocument. Then pass the FlowDocument to the UpdateViewer dialog and show it modally so as to avoid interuppting the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result == null)       //Exception of some kind...
            {
                return;
            }
            ArrayList resultList = (ArrayList)e.Result;
            if (resultList.Count == 1)      //No update
            {
                if ((bool)resultList[0])        //Get back ShowCompleted flag
                {
                    MessageBox.Show(this, Properties.Resources.NoUpdatesAvailableStr, Properties.Resources.NoUpdatesAvailableShortStr, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            else if (resultList.Count == 2)
            {
                FlowDocument fd = (FlowDocument)XamlReader.Parse((String)resultList[1]);
                UpdateViewer updateViewer = new UpdateViewer(Properties.Resources.NewUpdateAvailableStr, fd) { Owner = this };
                updateViewer.Show();
            }
        }

        #endregion Check for Updates code
        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Calls the PropertyChanged event which tells the UI to re-check the velue of the Property whose name is passed.
        /// </summary>
        /// <param name="info">The property that the UI should re-check.</param>
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        #endregion INotifyPropertyChanged Members

        #endregion Methods

        #region Command Event Handlers
        /// <summary>
        /// The Exit command was executed. Currently only possible in the File menu.
        /// Exits the program safely by calling Exit() method
        /// </summary>
        private void ExitCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Exit();
        }
        /// <summary>
        /// Checked by the UI to know when the Exit command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// Exit should always be possible except if the WbfsDrive instance is busy.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void ExitCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Load drive command was executed. Call the LoadDrive method, passing the drive letter that was selected by the user.
        /// Load the drive stats.
        /// </summary>
        private void LoadDriveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!LoadDrive(((String)DriveComboBox.SelectedValue).Substring(0, 1), wbfsDriveCurrent))  //Load the drive that was selected (without the semicolon)
            {
                MainGrid.IsEnabled = false;                                         //Disable the main portion of the interface if loading the drive failed.
                SecondaryDriveButtonGrid.IsEnabled = false;                         //Disable the buttons for the secondary drive (disallows loading).
                return;
            }
            MainGrid.IsEnabled = true;      //Enable the main portion of the UI (under the load button and combo box)
            SecondaryDriveButtonGrid.IsEnabled = true;      //Enable the buttons for the secondary drive (allows loading).
            LoadDriveInfo();                                                        //Load the drive stats if the drive was loaded succesfully.
            SyncDriveComboBoxes();
        }

        /// <summary>
        /// Checked by the UI to know when the LoadDrive command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// LoadDrive should always be possible except if the WbfsDrive instance is busy.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void LoadDriveCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Extract command was executed. Ask the user what filename he wants to save as, or which directory to save to (if more than one item is selected).
        /// Start the asynchronous background worker to do the extraction process. Show the progress dialog box modally. (keeps the user from doing anything else but keeps the UI live).
        /// </summary>
        private void ExtractCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (EntryListBox.SelectedIndex == -1 || EntryListBox.Items.Count == 0)          //No items in the Entries on Drive list or no items selected.
            {                                                                               //Show an error message
                MessageBox.Show(this, Properties.Resources.ErrorExtractingNoEntriesStr, Properties.Resources.ErrorExtractingNoEntriesShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (EntryListBox.SelectedItems.Count == 1)                                      //If only 1 item is selected
            {
                SaveFileDialog sfd = new SaveFileDialog();                                  //Show a dialog asking the user for a filename.
                sfd.AddExtension = true;
                sfd.DefaultExt = "iso";                                                     //Use the default "iso" extension if the user doesnt enter one.
                sfd.Filter = "ISO image (*.iso)|*.iso";
                sfd.Title = Properties.Resources.SelectLocationExtractStr;
                sfd.CheckPathExists = true;                                                 //Make sure the path exists.
                sfd.DereferenceLinks = true;                                                //Give the target for shortcuts
                sfd.ValidateNames = true;                                                   //only allow valid windows filenames.
                sfd.OverwritePrompt = true;

                String itemFilename = ((WbfsEntry)EntryListBox.SelectedItem).EntryName + ".iso";                      //Create a filename for the iso using the game's name

                foreach (char invalidChar in Path.GetInvalidFileNameChars())        //Strip out the invalid filename characters from the game's name
                {
                    itemFilename = itemFilename.Replace(invalidChar, '_');
                }
                sfd.FileName = itemFilename;

                bool result = sfd.ShowDialog(this).GetValueOrDefault(false);                //Show the dialog modally, default the result of the dialog to false (cancel).
                if (!result)                                                                //If the user cancels out, stop and do nothing
                    return;

                BackgroundWorker bw = new BackgroundWorker();                               //Create a background worker and hook up the events (callbacks)
                bw.DoWork += new DoWorkEventHandler(bwExtract_DoWork);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwExtract_RunWorkerCompleted);
                bw.ProgressChanged += new ProgressChangedEventHandler(bwExtract_ProgressChanged);
                bw.WorkerReportsProgress = true;                                            //Worker should report progress
                bw.WorkerSupportsCancellation = true;                                      //Cancellation is not allowed (no way of cancelling in libwbfs for now)
                ArrayList argList = new ArrayList();  //Create the list of arguments needed for the extraction processs bwExtract_DoWork
                argList.Add(sfd.FileName);                                                  //First index, the filename selected by the user
                argList.Add(EntryListBox.SelectedItems);                                    //second index, the list of files selected by the user (only one in this case, but send the selectedItems and let the extraction method handle that)
                argList.Add(bw);                                                            //third index, the background worker object to allow progress reporting
                bw.RunWorkerAsync(argList);                                                 //Asnychronously call bwExtract_DoWork and pass argument list as the e.Arguments.

                pd = new ProgressDialog(Properties.Resources.ExtractionProgressStr, false, false, 1, false, null);       //Create a progress dialog in single item mode, with no overall progress 
                pd.Owner = this;                                                                            //Set the dialog's owner to this window to keep it centered.
                pd.ShowDialog();                                                                            //Show it modally
            }
            else                    //More than 1 item, so ask for a folder and send all selected items
            {
                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog { Description = Properties.Resources.SelectDirectoryToExtractToStr, ShowNewFolderButton = true };  //Show a folder selection dialog
                if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK)   //Check if user clicked anything but OK, if so stop and return.
                    return;
                if (!Directory.Exists(fbd.SelectedPath))                        //Check if the directory the user selected actually exists. If not, show an error and return
                {
                    MessageBox.Show(this, Properties.Resources.ErrorInvalidFolderStr, Properties.Resources.ErrorInvalidFolderShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                //Make sure the user has write access to the specified path.
                string tempFile = IOPath.Combine(fbd.SelectedPath, IOPath.GetRandomFileName());             //Create a random filename in the selected directory
                try
                {
                    File.Create(tempFile).Close();                                                          //Create a file with that random filename there and close it immediately
                }
                catch
                {                                                                                           //If an exception is caught, the user or application doesn't have write access to that directory, show an error and stop
                    MessageBox.Show(this, Properties.Resources.ErrorInvalidFolderStr, Properties.Resources.ErrorInvalidFolderShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                File.Delete(tempFile);                                                                      //If the user did have write access, then the file was created and closed, so delete it now.

                BackgroundWorker bw = new BackgroundWorker();                                               //Create a background worker and hook up the events (callbacks)
                bw.DoWork += new DoWorkEventHandler(bwExtract_DoWork);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwExtract_RunWorkerCompleted);
                bw.ProgressChanged += new ProgressChangedEventHandler(bwExtract_ProgressChanged);
                bw.WorkerReportsProgress = true;                                                            //It should allow reporting progress
                bw.WorkerSupportsCancellation = true;                                                      //But not cancellation (no way to do this in libwbfs at the moment)
                ArrayList argList = new ArrayList();                                                        //Create the list of arguments for bwExtract_DoWork
                argList.Add(fbd.SelectedPath);                                                              //First index, the folder the user selected
                argList.Add(EntryListBox.SelectedItems);                                                    //Second index, the items that the user selected
                argList.Add(bw);                                                                            //Third index, the background worker, to allow progress reporting
                bw.RunWorkerAsync(argList);                                                                 //Asynchronously call bwExtract_DoWork to do the extraction

                pd = new ProgressDialog(Properties.Resources.ExtractionProgressStr, false, true, EntryListBox.SelectedItems.Count, true, bw);     //Create a progress dialog with overall progress enabled and the total number of items set to the number of selected items
                pd.Owner = this;                                                                           //Set the owner of the dialog to this window to keep it centered
                pd.ShowDialog();                                                                            //Show the dialog modally to keep the user from doing anything else with the program while extracting
            }
        }

        /// <summary>
        /// Checked by the UI to know when the Extract command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// Extract shouldn't be possible if the WbfsDrive instance is busy, or if there are no items in the Entries on Drive, or if no items are selected, or if the drive hasn't been loaded yet.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void ExtractCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy || EntryListBox.SelectedIndex == -1 || EntryListBox.Items.Count == 0 || wbfsDriveCurrent.LoadedDriveLetter.Equals(String.Empty))
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Add command was exected. Check if the drive has been loaded, if there are entries to add the call the AddToDrive method asynchronously.
        /// </summary>
        private void AddCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.LoadedDriveLetter.Equals(String.Empty))            //Check if the drive has been loaded yet, if not show an error message.
            {
                MessageBox.Show(this, Properties.Resources.NoDriveSelectedStr, Properties.Resources.NoDriveSelectedShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (wbfsDriveCurrent.EntriesToAdd.Count == 0)                           //Check if there are any entries in the Entries To Add list, if not show an error message.
            {
                MessageBox.Show(this, Properties.Resources.NoIsoSelectedToAddStr, Properties.Resources.NoIsoSelectedToAddShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            BackgroundWorker bw = new BackgroundWorker();                           //Create a background worker and hook up the events (callbacks)
            bw.DoWork += new DoWorkEventHandler(bwAddToDrive_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwAddToDrive_RunWorkerCompleted);
            bw.ProgressChanged += new ProgressChangedEventHandler(bwAddToDrive_ProgressChanged);
            bw.WorkerReportsProgress = true;                                        //Worker should allow progress reporting.
            bw.WorkerSupportsCancellation = true;
            bw.RunWorkerAsync(wbfsDriveCurrent.EntriesToAdd);                       //Asynchronously call bwAddToDrive_DoWork passing the entire list of entries to be added.

            pd = new ProgressDialog(Properties.Resources.CopyProgressStr, false, true, wbfsDriveCurrent.EntriesToAdd.Count, true, bw);    //Create a progress dialog and allow overall progres, setting the number of entries to be done.
            pd.Owner = this;                                                        //Set the owner of the dialog to this window to keep it centered.
            pd.ShowDialog();                                                        //Show the dialog modally to keep the user from interacting with the UI, while still keeping the UI live.
        }

        /// <summary>
        /// Checked by the UI to know when the Add command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// Add shouldn't be possible if the WbfsDrive instance is busy, there are no entries to add in the list, or no drive has been loaded.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void AddCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy || (wbfsDriveCurrent.EntriesToAdd != null && wbfsDriveCurrent.EntriesToAdd.Count == 0) || wbfsDriveCurrent.LoadedDriveLetter.Equals(String.Empty))
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Delete command was executed. Check if any items were selected and that the drive was loaded, the delete each entry.
        /// </summary>
        private void DeleteCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (EntryListBox.SelectedIndex == -1 || EntryListBox.Items.Count == 0)  //Make sure there are some items in the list and that at least one is selected.
                return;
            if (wbfsDriveCurrent.LoadedDriveLetter.Equals(String.Empty))            //Make sure the drive has been loaded.
                return;
            WbfsEntry[] entries = new WbfsEntry[EntryListBox.SelectedItems.Count];
            EntryListBox.SelectedItems.CopyTo(entries, 0);                          //Copy the selected entries to a new list (necessary to avoid the size of the list changing inside the foreach loop)
            foreach (WbfsEntry item in entries)
            {                                                                       //For each selected entry, confirm deletion and the ask the WbfsDrive instance to delete that game from the disc
                MessageBoxResult result = MessageBox.Show(this, Properties.Resources.ConfirmDeleteStr + item.EntryName + "?", Properties.Resources.ConfirmDeleteShortStr, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (result != MessageBoxResult.Yes)                                  //If the user answered anything but yes, cancel the deletion for this item, move on to the next item
                    continue;
                ErrorDetails resultDelete = wbfsDriveCurrent.DeleteGameFromDisc(item);  //Call the DeleteGameFromDisc method, passing the entry's object. This automatically updates the EntriesListBox since it's bound to EntriesOnDrive.
                if (resultDelete != null)                                               //If errors occured, show an error message, otherwise let the loop continue.
                    MessageBox.Show(this, resultDelete.LongMessage, resultDelete.ShortMessage, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            NotifyPropertyChanged("PercentDiskSpaceUsed");
            NotifyPropertyChanged("PercentDiskPlusToAdd");
        }

        /// <summary>
        /// Checked by the UI to know when the Delete command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// Delete shouldn't be possible if the WbfsDrive instance is busy, there are no items in the Entries On Drive list, no items are selected, or no drive was loaded.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void DeleteCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy || EntryListBox.SelectedIndex == -1 || EntryListBox.Items.Count == 0 || wbfsDriveCurrent.LoadedDriveLetter.Equals(String.Empty))
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Remove command was executed. Make sure an item was actually selected, then remove it from the Entries To Add list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (AddListBox.SelectedIndex == -1 || AddListBox.Items.Count <= 0 || AddListBox.SelectedItems.Count != 1)      //Make sure a single item was selected. (only one item is selectable on the AddListBox)
            {
                MessageBox.Show(this, Properties.Resources.MustSelectEntryToRemoveStr, Properties.Resources.MustSelectEntryToRemoveShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            WbfsEntry addListBoxSelectedItem = (WbfsEntry)AddListBox.SelectedItem;
            wbfsDriveCurrent.EntriesToAdd.Remove(addListBoxSelectedItem);       //Remove the item from the EntriesToAdd list. This automatically updates the ListBox since it's bound to EntriesToAdd.
            if (IOPath.GetDirectoryName(addListBoxSelectedItem.FilePath).Contains(wbfsDriveCurrent.TempDirectory))
            {
                try
                {
                    File.Delete(addListBoxSelectedItem.FilePath);                 //Get rid of any temporary iso files created from extracted archives.
                }
                catch (Exception exc)
                {
#if DEBUG
                    throw exc;
#endif
                }
            }
            NotifyPropertyChanged("EstimatedTotalSizeString");
            NotifyPropertyChanged("PercentDiskPlusToAdd");
        }

        /// <summary>
        /// Checked by the UI to know when the Remove command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// Remove shouldn't be possible if the WbfsDrive instance is busy, or no items are in the AddListBox or no items were selected in the AddListBox
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void RemoveCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy || AddListBox.SelectedIndex == -1 || AddListBox.Items.Count <= 0 || AddListBox.SelectedItems.Count != 1)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Browse command was executed. Show the user the open file dialog and add the selected items to the Entries To Add list, by calling ParseIsoFileList.
        /// </summary>
        private void BrowseCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();      //Create an open file dialog, with the filter set to ISO files.
            ofd.DefaultExt = "iso";
            ofd.Filter = "ISO image (*.iso)|*.iso|RAR archives (*.rar)|*.rar|All supported files (*.iso;*.rar)|*.iso;*.rar";
            ofd.FilterIndex = 3;
            ofd.Title = Properties.Resources.SelectImageToAddStr;
            ofd.CheckFileExists = true;                     //Make sure the selected file(s) exist.
            ofd.Multiselect = true;                         //Allow selecting multiple files.
            bool result = ofd.ShowDialog(this).GetValueOrDefault(false);        //Show the dialog, and defalt the result to false (cancel).
            if (!result)                                    //If the user cancelled, stop and return.
                return;

            BackgroundWorker bw = new BackgroundWorker();                                       //Create a background worker to do the parsing and archive extraction
            bw.DoWork += new DoWorkEventHandler(bw_ParseDoWork);                                //hook up event handlers
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ParseProgressChanged);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_ParseRunWorkerCompleted);
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            ArrayList argList = new ArrayList { ofd.FileNames, bw, Properties.Settings.Default.EnableRarExtraction };
            bw.RunWorkerAsync(argList);                                                       //Pass the list of files dropped as well as the background worker object to allow progress reporting

            pd = new ProgressDialog(Properties.Resources.ReadingFilesText, false, true, ofd.FileNames.Length, true, bw);     //Create a progress dialog with overall progress enabled and the total number of items set to the number of files
            pd.Owner = this;                                                                           //Set the owner of the dialog to this window to keep it centered
            pd.ShowDialog();                                                                            //Show the dialog modally to keep the user from doing anything else with the program while parsing
        }

        /// <summary>
        /// Checked by the UI to know when the Browse command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// Browse should always be possible except if the WbfsDrive instance is busy or if no drive has been loaded.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void BrowseCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy || wbfsDriveCurrent.LoadedDriveLetter.Equals(String.Empty))
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Clears the list of EntriesToAdd (automatically updating the AddListBox since it's bound to it).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            wbfsDriveCurrent.EntriesToAdd.Clear();
            String[] tempFiles = Directory.GetFiles(wbfsDriveCurrent.TempDirectory, "*.iso", SearchOption.TopDirectoryOnly);
            foreach (String item in tempFiles)
            {
                try
                {
                    File.Delete(item);          //Get rid of any temporary iso files created from extracted archives.
                }
                catch (Exception exc)
                {
#if DEBUG
                throw exc;
#endif
                }
            }
            NotifyPropertyChanged("EstimatedTotalSizeString");
            NotifyPropertyChanged("PercentDiskPlusToAdd");
        }

        /// <summary>
        /// Checked by the UI to know when the Clear command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// Clear should always be possible except if the WbfsDrive instance is busy, or there are no entries in the Entries To Add list.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void ClearCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy || (wbfsDriveCurrent.EntriesToAdd != null && wbfsDriveCurrent.EntriesToAdd.Count == 0))
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Format command was executed. Make sure an item is selected in the drive combo box, prompt the user, the ask WbfsDrive instance to format.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormatCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (DriveComboBox.SelectedIndex == -1 || DriveComboBox.Items.Count == 0)    //Make sure a drive has been selected from the combo box.
                return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Properties.Resources.FormatWarningP1Str + DriveComboBox.SelectedItem +
                           Properties.Resources.FormatWarningP2Str);
            sb.AppendLine(Properties.Resources.FormatWarningP3Str + DriveComboBox.SelectedItem + "?");
            MessageBoxResult result = MessageBox.Show(this, sb.ToString(),
                Properties.Resources.FormatWarningShortStr, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (result != MessageBoxResult.Yes)             //Prompt the user with a warning message. If the result is anything but yes, cancel out and return.
                return;

            ErrorDetails resultFormat = wbfsDriveCurrent.FormatWbfsDrive(((String)DriveComboBox.SelectedValue).Substring(0, 1));    //Ask WbfsDrive instance to format the drive (without the semicolon), get the error details.
            if (resultFormat != null)       //If errors did occur, show an error message with the details and return.
            {
                MessageBox.Show(this, resultFormat.LongMessage, resultFormat.ShortMessage, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            //If no errors occured, tell the user the format process was successful.
            MessageBox.Show(this, Properties.Resources.FormatSuccessfulStr, Properties.Resources.FormatSuccessfulShortStr, MessageBoxButton.OK, MessageBoxImage.Information);
            NotifyPropertyChanged("PercentDiskSpaceUsed");
            NotifyPropertyChanged("PercentDiskPlusToAdd");
        }

        /// <summary>
        /// Checked by the UI to know when the Format command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// Format should always be possible except if the WbfsDrive instance is busy.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void FormatCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// About command was executed. Show the About dialog.
        /// </summary>
        private void AboutCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow { Owner = this };
            aboutWindow.ShowDialog();
        }

        /// <summary>
        /// Checked by the UI to know when the About command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// About should always be possible except if the WbfsDrive instance is busy.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void AboutCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Rename On Add command was executed. Make sure an item was selected in the Add ListBox then prompt the user for the new name and update the name.
        /// Can only be done on a single item at a time.
        /// </summary>
        private void RenameOnAddCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (AddListBox.SelectedIndex == -1 || AddListBox.Items.Count <= 0 || AddListBox.SelectedItems.Count != 1)      //Make sure a single item is selected and that the Add list isn't empty
            {
                MessageBox.Show(this, Properties.Resources.MustSelectEntryToRenameStr, Properties.Resources.MustSelectEntryToRenameShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            RenameDialog rd = new RenameDialog(((WbfsEntry)AddListBox.SelectedItem).EntryName);     //Create a rename dialog with the selected entriy's name.
            rd.Owner = this;                                                                        //Set the dialog's owner to this window to keep it centered.
            if (!rd.ShowDialog().GetValueOrDefault(false))                                          //Show the dialgo and default the result to false (cancel). If cancelled, stop and return.
                return;
            if (rd.NewNameValue.Trim().Length == 0 || rd.NewNameValue.Trim().Length > 57)           //Trim the users new name (removing leading and trailing spaces) and make sure its more than one character and less than 57 (without leading/trailing spaces)
            {                                                                                       //Show and error message if it is.
                MessageBox.Show(this, Properties.Resources.ErrorRenameInvalidStr, Properties.Resources.ErrorRenameInvalidShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            WbfsEntry tempEntry = ((WbfsEntry)AddListBox.SelectedItem);                             //Get the selected item's object
            tempEntry.EntryName = rd.NewNameValue;                                                  //Update it's name (causes both the EntriesToAdd list and the list box to be updated).
        }

        /// <summary>
        /// Checked by the UI to know when the RenameOnAdd command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// RenameOnAdd shouldn't be possible if the WbfsDrive instance is busy, or the number of items in the AddList box is zero, or no items are selected or more than one item is selected.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void RenameOnAddCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy || AddListBox.SelectedIndex == -1 || AddListBox.Items.Count <= 0 || AddListBox.SelectedItems.Count != 1)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// RenameOnDrive command was executed. Call the RenameOnDrive method after checking that a drive was loaded.
        /// </summary>
        private void RenameOnDriveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (EntryListBox.SelectedIndex == -1 || EntryListBox.Items.Count == 0)      //Make sure there are items in the Entries On Drive, and that an item was selected.
                return;
            if (wbfsDriveCurrent.LoadedDriveLetter.Equals(String.Empty))                //Make sure the drive has been loaded.
                return;
            WbfsEntry tempEntry = (WbfsEntry)EntryListBox.SelectedItem;                 //Get the selected item's object and call this.RenameOnDisc method. Automatically updates both the EntriesOnDrive list and the EntryListBox.
            RenameDiscOnDrive(tempEntry);
        }

        /// <summary>
        /// Checked by the UI to know when the RenameOnDrive command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// RenameOnDrive shouldn't be possible if the WbfsDrive instance is busy, or there are no entries in the EntriesOnDrive list, or the number of selected items is more than one or no items are selected, or the drive hasn't been loaded.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void RenameOnDriveCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy || EntryListBox.SelectedIndex == -1 || EntryListBox.Items.Count == 0 || EntryListBox.SelectedItems.Count > 1 || wbfsDriveCurrent.LoadedDriveLetter.Equals(String.Empty))
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Sort By Name command executed. Set the list box's sort mode and update the check boxes.
        /// </summary>
        private void SortByNameCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (((String)e.Parameter).Equals(ONDRIVECONTEXTPARAM))      //If the ONDRIVECONTEXTPARAM parameter was sent by the command, then the command was executed on the
            {                                                           //Entries on drive ListBox. Update that listbox's sort mode and update the checkboxes.
                //Set list to be sorted by name.
                ICollectionView listView = CollectionViewSource.GetDefaultView(wbfsDriveCurrent.EntriesOnDrive);
                ListCollectionView listCollectionView = (ListCollectionView)listView;
                listCollectionView.CustomSort = new NameSorter();
                SortByNameOnDriveMenuItem.IsChecked = true;
                SortByIdOnDriveMenuItem.IsChecked = false;
                SortBySizeOnDriveMenuItem.IsChecked = false;
                SortByIndexOnDriveMenuItem.IsChecked = false;
            }
            else                                                        //Otherwise the command was executed on the AddTo ListBox so update that ListBox's sort mode and checkboxes.
            {
                //Set list to be sorted by name.
                ICollectionView listView = CollectionViewSource.GetDefaultView(wbfsDriveCurrent.EntriesToAdd);
                ListCollectionView listCollectionView = (ListCollectionView)listView;
                listCollectionView.CustomSort = new NameSorter();
                SortByNameToAddMenuItem.IsChecked = true;
                SortByIdToAddMenuItem.IsChecked = false;
                SortBySizeToAddMenuItem.IsChecked = false;
                SortByStatusToAddMenuItem.IsChecked = false;
                SortByFilePathToAddMenuItem.IsChecked = false;
            }
        }

        /// <summary>
        /// Checked by the UI to know when the SortByName command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// SortByName shouldn't be possible if the WbfsDrive instance is busy or the number of entries on the corresponding list box is zero.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void SortByNameCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy)
            {
                e.CanExecute = false;
                return;
            }
            if (((String)e.Parameter).Equals(ONDRIVECONTEXTPARAM))
            {
                if (wbfsDriveCurrent.EntriesOnDrive.Count == 0)
                {
                    e.CanExecute = false;
                    return;
                }
            }
            else
            {
                if (wbfsDriveCurrent.EntriesToAdd.Count == 0)
                {
                    e.CanExecute = false;
                    return;
                }
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Sort By ID command executed. Set the list box's sort mode and update the check boxes.
        /// </summary>
        private void SortByIdCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (((String)e.Parameter).Equals(ONDRIVECONTEXTPARAM))
            {
                //Set list to be sorted by ID.
                ICollectionView listView = CollectionViewSource.GetDefaultView(wbfsDriveCurrent.EntriesOnDrive);
                ListCollectionView listCollectionView = (ListCollectionView)listView;
                listCollectionView.CustomSort = new IdSorter();
                SortByNameOnDriveMenuItem.IsChecked = false;
                SortByIdOnDriveMenuItem.IsChecked = true;
                SortBySizeOnDriveMenuItem.IsChecked = false;
                SortByIndexOnDriveMenuItem.IsChecked = false;
            }
            else
            {
                //Set list to be sorted by ID.
                ICollectionView listView = CollectionViewSource.GetDefaultView(wbfsDriveCurrent.EntriesToAdd);
                ListCollectionView listCollectionView = (ListCollectionView)listView;
                listCollectionView.CustomSort = new IdSorter();
                SortByNameToAddMenuItem.IsChecked = false;
                SortByIdToAddMenuItem.IsChecked = true;
                SortBySizeToAddMenuItem.IsChecked = false;
                SortByStatusToAddMenuItem.IsChecked = false;
                SortByFilePathToAddMenuItem.IsChecked = false;
            }
        }

        /// <summary>
        /// Checked by the UI to know when the SortByID command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// SortByID shouldn't be possible if the WbfsDrive instance is busy or the number of entries on the corresponding list box is zero.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void SortByIdCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy)
            {
                e.CanExecute = false;
                return;
            }
            if (((String)e.Parameter).Equals(ONDRIVECONTEXTPARAM))
            {
                if (wbfsDriveCurrent.EntriesOnDrive.Count == 0)
                {
                    e.CanExecute = false;
                    return;
                }
            }
            else
            {
                if (wbfsDriveCurrent.EntriesToAdd.Count == 0)
                {
                    e.CanExecute = false;
                    return;
                }
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Sort By Size command executed. Set the list box's sort mode and update the check boxes.
        /// </summary>
        private void SortBySizeCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (((String)e.Parameter).Equals(ONDRIVECONTEXTPARAM))
            {
                //Set lists to be sorted by size.
                ICollectionView listView = CollectionViewSource.GetDefaultView(wbfsDriveCurrent.EntriesOnDrive);
                ListCollectionView listCollectionView = (ListCollectionView)listView;
                listCollectionView.CustomSort = new SizeSorter();
                SortByNameOnDriveMenuItem.IsChecked = false;
                SortByIdOnDriveMenuItem.IsChecked = false;
                SortBySizeOnDriveMenuItem.IsChecked = true;
                SortByIndexOnDriveMenuItem.IsChecked = false;
            }
            else
            {
                //Set lists to be sorted by size.
                ICollectionView listView = CollectionViewSource.GetDefaultView(wbfsDriveCurrent.EntriesToAdd);
                ListCollectionView listCollectionView = (ListCollectionView)listView;
                listCollectionView.CustomSort = new SizeSorter();
                SortByNameToAddMenuItem.IsChecked = false;
                SortByIdToAddMenuItem.IsChecked = false;
                SortBySizeToAddMenuItem.IsChecked = true;
                SortByStatusToAddMenuItem.IsChecked = false;
                SortByFilePathToAddMenuItem.IsChecked = false;
            }
        }

        /// <summary>
        /// Checked by the UI to know when the SortBySize command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// SortBySize shouldn't be possible if the WbfsDrive instance is busy or the number of entries on the corresponding list box is zero.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void SortBySizeCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy)
            {
                e.CanExecute = false;
                return;
            }
            if (((String)e.Parameter).Equals(ONDRIVECONTEXTPARAM))
            {
                if (wbfsDriveCurrent.EntriesOnDrive.Count == 0)
                {
                    e.CanExecute = false;
                    return;
                }
            }
            else
            {
                if (wbfsDriveCurrent.EntriesToAdd.Count == 0)
                {
                    e.CanExecute = false;
                    return;
                }
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Sort By Status command executed. Set the list box's sort mode and update the check boxes.
        /// </summary>
        private void SortByStatusCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Set list to be sorted by status. (only possible on entries to add list).
            ICollectionView listView = CollectionViewSource.GetDefaultView(wbfsDriveCurrent.EntriesToAdd);
            ListCollectionView listCollectionView = (ListCollectionView)listView;
            listCollectionView.CustomSort = new StatusSorter();
            SortByNameToAddMenuItem.IsChecked = false;
            SortByIdToAddMenuItem.IsChecked = false;
            SortBySizeToAddMenuItem.IsChecked = false;
            SortByStatusToAddMenuItem.IsChecked = true;
            SortByFilePathToAddMenuItem.IsChecked = false;
        }

        /// <summary>
        /// Checked by the UI to know when the SortByStatus command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// SortByStatus shouldn't be possible if the WbfsDrive instance is busy or the number of entries on the AddTo ListBox is zero.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void SortByStatusCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy || wbfsDriveCurrent.EntriesToAdd.Count == 0)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Sort By Index command executed. Set the list box's sort mode and update the check boxes. Sorts entries by their location on the WBFS Drive.
        /// Only possible on the EntriesOnDrive list.
        /// </summary>
        private void SortByIndexCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Set list to be sorted by index. (only possible on entries on drive list.)
            ICollectionView listView = CollectionViewSource.GetDefaultView(wbfsDriveCurrent.EntriesOnDrive);
            ListCollectionView listCollectionView = (ListCollectionView)listView;
            listCollectionView.CustomSort = new IndexSorter();
            SortByNameOnDriveMenuItem.IsChecked = false;
            SortByIdOnDriveMenuItem.IsChecked = false;
            SortBySizeOnDriveMenuItem.IsChecked = false;
            SortByIndexOnDriveMenuItem.IsChecked = true;
        }

        /// <summary>
        /// Checked by the UI to know when the SortByIndex command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// SortByIndex shouldn't be possible if the WbfsDrive instance is busy or the number of entries on the EntriesOnDrive ListBox is zero.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void SortByIndexCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy || wbfsDriveCurrent.EntriesOnDrive.Count == 0)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Sort By File Path command executed. Set the list box's sort mode and update the check boxes. Sorts entries by their file path on the computer.
        /// Only possible on the AddTo ListBox
        /// </summary>
        private void SortByFilePathCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Set list to be sorted by file path. (only possible on entries to add list).
            ICollectionView listView = CollectionViewSource.GetDefaultView(wbfsDriveCurrent.EntriesToAdd);
            ListCollectionView listCollectionView = (ListCollectionView)listView;
            listCollectionView.CustomSort = new FilePathSorter();
            SortByNameToAddMenuItem.IsChecked = false;
            SortByIdToAddMenuItem.IsChecked = false;
            SortBySizeToAddMenuItem.IsChecked = false;
            SortByStatusToAddMenuItem.IsChecked = false;
            SortByFilePathToAddMenuItem.IsChecked = true;
        }

        /// <summary>
        /// Checked by the UI to know when the SortByFilePath command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// SortByFilePath shouldn't be possible if the WbfsDrive instance is busy or the number of entries on the EntriesToAdd list is zero.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void SortByFilePathCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy || wbfsDriveCurrent.EntriesToAdd.Count == 0)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Set Covers Directory command executed. Show a folder browser dialog and set the cover directory to the selected folder.
        /// Covers directory is only used for loading covers, not for storing downloaded covers.
        /// </summary>
        private void SetCoversDirCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();

            fbd.Description = Properties.Resources.SelectDirForHBCStr;
            fbd.ShowNewFolderButton = true;

            if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK)       // Show a folder browser dilaog asking the user for the cover directory they'd like
            {                                                                   //If the result is anything but OK, cancel out and return.
                return;
            }

            string dir_path = fbd.SelectedPath.Trim();
            if (string.IsNullOrEmpty(dir_path))                       //Make sure the selected directory isn't empty.
            {
                MessageBox.Show(this, Properties.Resources.MustSelectExistingFolderStr, Properties.Resources.MustSelectExistingFolderShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Properties.Settings.Default.CoverDirs.Add(dir_path);                    //Set the cover directory setting for the user.
            Properties.Settings.Default.Save();                                 //Save the user's setting to disk.
            if (wbfsDriveCurrent.EntriesToAdd.Count > 0 || wbfsDriveCurrent.EntriesOnDrive.Count > 0)       //If there are any entries in either of the lists, update the cover images with any new images that may be available.
            {
                wbfsDriveCurrent.UpdateCoverImages();
            }
        }

        /// <summary>
        /// Checked by the UI to know when the SetCoversDir command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// SetCoversDir should always be possible except if the WbfsDrive instance is busy.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void SetCoversDirCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Show the options menu letting the user set the temp dir, cover dirs, download covers option and show covers option. Update the corresponding values afterwards.
        /// </summary>
        private void ShowOptionsCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ShowOptionsDialog();
        }

        /// <summary>
        /// Checked by the UI to know when the ShowOptions command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// ShowOptions should always be possible except if the WbfsDrive instance is busy.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void ShowOptionsCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Show Covers command was executed. Toggle showing covers on or off, based on the current status.
        /// </summary>
        private void ShowCoversCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Properties.Settings.Default.ShowCovers = !Properties.Settings.Default.ShowCovers;       //Set the setting to the opposite of what it is now.
            Properties.Settings.Default.Save();                                                     //Save the setting to disk
            EditShowCoversMenuItem.IsChecked = Properties.Settings.Default.ShowCovers;              //Update the menu's checkbox
        }

        /// <summary>
        /// Checked by the UI to know when the ShowCovers command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// ShowCovers should always be possible except if the WbfsDrive instance is busy.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void ShowCoversCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// MakeSingleHBC command was executed. Ask the user for the target directory and call this.CreateHbcEntry to create the necessary files.
        /// </summary>
        private void MakeSingleHBCCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (((String)e.Parameter).Equals(ONDRIVECONTEXTPARAM))          //Was executed on the Entries On Drive list.
            {
                WbfsEntry entry = (WbfsEntry)EntryListBox.SelectedItem;     //Get the selected item and make sure it's not null.
                if (entry == null)
                    return;
                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();      //Create a folder browser dialog asking for the target directory.

                fbd.Description = Properties.Resources.SelectDirForHBCStr;
                fbd.ShowNewFolderButton = true;

                if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK)               //Show the dialog If the result is anything but OK, cancel and return.
                {
                    return;
                }

                string dir_path = fbd.SelectedPath.Trim();                                  //Make sure the selected path is not empty.
                if (string.IsNullOrEmpty(dir_path))
                {
                    MessageBox.Show(this, Properties.Resources.MustSelectExistingFolderStr, Properties.Resources.MustSelectExistingFolderShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!CreateHbcEntry(entry, dir_path))                                       //Call this.CreateHbcEntry with the selected entry and the target directory. If it fails show an error message.
                {
                    MessageBox.Show(this, Properties.Resources.ErrorCreatingHBCStr, Properties.Resources.ErrorCreatingHBCShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

            }
            else
            {                                                                   //Was executed on the Entries To Add list.
                WbfsEntry entry = (WbfsEntry)AddListBox.SelectedItem;           //Get the selected item and make sure it's not null.
                if (entry == null)
                    return;

                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();      //Create a folder browser dialog asking for the target directory.

                fbd.Description = Properties.Resources.SelectDirForHBCStr;
                fbd.ShowNewFolderButton = true;

                if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK)               //Show the dialog If the result is anything but OK, cancel and return.
                {
                    return;
                }

                string dir_path = fbd.SelectedPath.Trim();

                if (string.IsNullOrEmpty(dir_path))                               //Make sure the selected path is not empty.
                {
                    MessageBox.Show(this, Properties.Resources.MustSelectExistingFolderStr, Properties.Resources.MustSelectExistingFolderShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!CreateHbcEntry(entry, dir_path))                                       //Call this.CreateHbcEntry with the selected entry and the target directory. If it fails show an error message.
                {
                    MessageBox.Show(this, Properties.Resources.ErrorCreatingHBCStr, Properties.Resources.ErrorCreatingHBCShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            //Show completion message (successful, since it got this far).
            MessageBox.Show(this, Properties.Resources.CompletedHBCEntryStr, Properties.Resources.CompletedStr, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Checked by the UI to know when the MakeSingleHBC command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// MakeSingleHBC shouldn't be possible if the WbfsDrive instance is busy, or the corresponding list is empty, or no item is selected.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void MakeSingleHBCCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy)
            {
                e.CanExecute = false;
                return;
            }
            if (((String)e.Parameter).Equals(ONDRIVECONTEXTPARAM))
            {
                if (wbfsDriveCurrent.EntriesOnDrive.Count == 0 || EntryListBox.SelectedIndex < 0 || EntryListBox.SelectedItem == null)
                {
                    e.CanExecute = false;
                    return;
                }
            }
            else
            {
                if (wbfsDriveCurrent.EntriesToAdd.Count == 0 || AddListBox.SelectedIndex < 0 || AddListBox.SelectedItem == null || AddListBox.SelectedItems.Count != 1)
                {
                    e.CanExecute = false;
                    return;
                }
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// MakeAllHBC command was executed. Ask the user for the target directory and call this.CreateHbcEntry for all the entries in the corresponding list box.
        /// </summary>
        private void MakeAllHBCCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (((String)e.Parameter).Equals(ONDRIVECONTEXTPARAM))          //Was executed on the Entries on Drive list.
            {
                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();      //Create a folder browser dialog asking for the target directory.

                fbd.Description = Properties.Resources.SelectDirForHBCStr;
                fbd.ShowNewFolderButton = true;

                if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK)                   //Show the dialog If the result is anything but OK, cancel and return.
                {
                    return;
                }

                string dir_path = fbd.SelectedPath.Trim();
                if (string.IsNullOrEmpty(dir_path))                                   //Make sure the selected path is not empty.
                {
                    MessageBox.Show(this, Properties.Resources.MustSelectExistingFolderStr, Properties.Resources.MustSelectExistingFolderShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                foreach (WbfsEntry item in wbfsDriveCurrent.EntriesOnDrive)                 //For each entry in the list call this.CreateHbcEntry and show an error message if an error occurs, then continue to the next.
                {
                    if (!CreateHbcEntry(item, dir_path))
                    {
                        MessageBox.Show(this, Properties.Resources.ErrorCreatingHBCStr, Properties.Resources.ErrorCreatingHBCShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }
                }
            }
            else
            {                                                             //Was executed on the Entries To Add list.
                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog { Description = Properties.Resources.SelectDirForHBCStr, ShowNewFolderButton = true };  //Create a folder browser dialog asking for the target directory.

                if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK)                   //Show the dialog If the result is anything but OK, cancel and return.
                {
                    return;
                }

                string dir_path = fbd.SelectedPath.Trim();
                if (string.IsNullOrEmpty(dir_path))                               //Make sure the selected path is not empty.
                {
                    MessageBox.Show(this, Properties.Resources.MustSelectExistingFolderStr, Properties.Resources.MustSelectExistingFolderShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                foreach (WbfsEntry item in wbfsDriveCurrent.EntriesToAdd)               //For each entry in the list call this.CreateHbcEntry and show an error message if an error occurs, then continue to the next.
                {
                    if (!CreateHbcEntry(item, dir_path))
                    {
                        MessageBox.Show(this, Properties.Resources.ErrorCreatingHBCStr, Properties.Resources.ErrorCreatingHBCShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }
                }
            }
            MessageBox.Show(this, Properties.Resources.CompletedHBCEntryStr, Properties.Resources.CompletedStr, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Checked by the UI to know when the MakeAllHBC command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// MakeAllHBC shouldn't be possible if the WbfsDrive instance is busy, or the number of entries in the corresponding list is zero.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void MakeAllHBCCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy)
            {
                e.CanExecute = false;
                return;
            }
            if (((String)e.Parameter).Equals(ONDRIVECONTEXTPARAM))
            {
                if (wbfsDriveCurrent.EntriesOnDrive.Count == 0)
                {
                    e.CanExecute = false;
                    return;
                }
            }
            else
            {
                if (wbfsDriveCurrent.EntriesToAdd.Count == 0)
                {
                    e.CanExecute = false;
                    return;
                }
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// ChangeLanguage command was executed. Change the language of the UI to the selected language (passed as two letter culture name in the command parameter).
        /// </summary>
        private void ChangeLanguageCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CultureInfo newCulture;
            try
            {
                newCulture = new CultureInfo((String)e.Parameter);      //Try and create a CultureInfo object from the command parameter (which should be the two letter culture name), 
            }                                                           //if an exception occurs, don't change the langage, the two-letter name must be wrong.
            catch
            {
                return;
            }
            try
            {
                Infralution.Localization.Wpf.CultureManager.UICulture = newCulture; //Auto-updates Thread.CurrentUICulture, which updates all the UI's language.
            }
            catch
            {
                if (newCulture.Name.Equals("zh-CHT"))
                    newCulture = CultureInfo.CreateSpecificCulture("zh-TW");
                else if (newCulture.Name.Equals("zh-CHS"))
                    newCulture = CultureInfo.CreateSpecificCulture("zh-CN");
                Infralution.Localization.Wpf.CultureManager.UICulture = newCulture;
            }
            Properties.Settings.Default.Language = (String)e.Parameter;         //Save the selected UI language to the settings file.
            Properties.Settings.Default.Save();                                 //Save the settings to disk.
            UpdateLanguageMenuCheckBoxes();                                     //Call UpdateLanguageMenuCheckBoxes to updat the check boxes in the language menu according to the saved setting.
        }

        /// <summary>
        /// Checked by the UI to know when the ChangeLanguage command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// ChangeLanguage should always be possible.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void ChangeLanguageCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        /// <summary>
        /// CheckForUpdates command was executed, launches browser and redirects to WBFS Manager blog site. Replace later with an integrated updating mechanism.
        /// </summary>
        private void CheckForUpdatesCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CheckForUpdates(true);
        }
        /// <summary>
        /// Checked by the UI to know when the CheckForUpdates command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// CheckForUpdates should always be possible.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void CheckForUpdatesCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        /// <summary>
        /// DownloadCoversFromWeb command was executed. Flip the settings flag and checkbox, update cover images.
        /// </summary>
        private void DownloadCoversFromWebCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Properties.Settings.Default.UseWebCovers = !Properties.Settings.Default.UseWebCovers;       //Flip the setting flag
            Properties.Settings.Default.Save();                                                         //Save the settings to disk
            EditDownloadCoversFromWebMenuItem.IsChecked = Properties.Settings.Default.UseWebCovers;     //Update the checkbox state on the menu.
            if (wbfsDriveCurrent.EntriesToAdd.Count > 0 || wbfsDriveCurrent.EntriesOnDrive.Count > 0)
            {                                                            //If there are any entries in either list, update the cover images (results in retrieval from web for missing covers if it was enabled).
                wbfsDriveCurrent.UpdateCoverImages();
            }
        }

        /// <summary>
        /// Checked by the UI to know when the DownloadCoversFromWeb command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// DownloadCoversFromWeb should always be possible except if the WbfsDrive instance is busy.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void DownloadCoversFromWebCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// ExportListToFile command was executed. Ask the user for the target filename and call ExportToListFile to write a CSV file with the selected items.
        /// </summary>
        private void ExportListToFileCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();      //Create a save file dialog, with filter set to .csv and default extension csv (if the user doesn't enter the extension)
            sfd.AddExtension = true;
            sfd.DefaultExt = "csv";
            sfd.Filter = "Comma-separated Values (*.csv)|*.csv";
            sfd.Title = Properties.Resources.SaveGameListAsStr;
            sfd.OverwritePrompt = false;
            bool result = sfd.ShowDialog(this).GetValueOrDefault(false);           //Show the dialog and default the result to false
            if (!result)                                                           //If the result was false (cancel), stop and return.
                return;
            if (EntryListBox.SelectedItems.Count <= 1 && EntryListBox.SelectedItems.Count >= 0)     //If there was exactly one file or no files selected, export the whole list
            {
                ExportToListFile(EntryListBox.Items, sfd.FileName);
            }
            else                                                                                    //If there were more than one files selected, export only the selected files.
            {
                ExportToListFile(EntryListBox.SelectedItems, sfd.FileName);
            }
        }

        /// <summary>
        /// Checked by the UI to know when the ExportListToFile command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// ExportListToFile should always be possible except if the WbfsDrive instance is busy.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void ExportListToFileCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy || EntryListBox.Items.Count == 0)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Expander was opened, resize the window to give pop-down effect.
        /// </summary>
        private void DriveToDriveExpander_Expanded(object sender, RoutedEventArgs e)
        {
            this.Height += 210;
        }

        /// <summary>
        /// Expander was closed, resize the window back to its normal size to give pop-up effect.
        /// </summary>
        private void DriveToDriveExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            this.Height -= 210;
        }

        /// <summary>
        /// Load the secondary drive and fill in stats.
        /// </summary>
        private void LoadSecondaryDriveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!LoadDrive(((String)SecondaryDriveComboBox.SelectedValue).Substring(0, 1), wbfsDriveSecondary))  //Load the drive that was selected (without the semicolon)
            {
                SecondaryDriveMainGrid.IsEnabled = false;           //Disable the main grid for the secondary drive since it didn't load correctly
                return;
            }
            SecondaryDriveMainGrid.IsEnabled = true;                //Enable the main grid for the secondary drive since it loaded successfully
            SyncDriveComboBoxes();
            LoadSecondaryDriveInfo();                               //Load the drive stats if the drive was loaded succesfully.
        }

        /// <summary>
        /// Checked by the UI to know when the LoadSecondaryDrive command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// LoadSecondaryDrive should always be possible except if one of the WbfsDrive instance are busy
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void LoadSecondaryDriveCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy || wbfsDriveSecondary.Busy)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// CloneToDrive command was executed. Check and make sure there are atually items to copy, there is a secondary drive loaded, theres enough space
        /// on the secondary drive and theres enough temporary space. Then call DriveToDriveCopy to do the actual copying of all entries on the source drive, using the TempDirectory setting.
        /// </summary>
        private void CloneToDriveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (EntryListBox.Items.Count == 0)          //No items in the Entries on Drive list.
            {                                                                               //Show an error message
                MessageBox.Show(this, Properties.Resources.ErrorNoEntriesOnSourceDriveStr, Properties.Resources.ErrorExtractingNoEntriesShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (wbfsDriveSecondary.LoadedDriveLetter.Equals(String.Empty))            //Check if the drive has been loaded yet, if not show an error message.
            {
                MessageBox.Show(this, Properties.Resources.ErrorNoSecondaryDriveLoadedStr, Properties.Resources.NoDriveSelectedShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (wbfsDriveCurrent.DriveStats.UsedSpace > wbfsDriveSecondary.DriveStats.FreeSpace)        //Check if the secondary (target) drive has enough space, if not show an error message
            {
                MessageBox.Show(this, Properties.Resources.ErrorNotEnoughSpaceToCloneStr, Properties.Resources.ErrorNotEnoughSpaceShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!Utils.CheckTempForSpace(Properties.Settings.Default.TempDirectory))                    //Make sure the temp folder has enough space to temporary hold each image file as its extracted and added to the secondary.
            {
                MessageBox.Show(this, Properties.Resources.ErrorNotEnoughSpaceTempFolderStr, Properties.Resources.ErrorNotEnoughSpaceShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            DriveToDriveCopy(wbfsDriveCurrent.EntriesOnDrive, Properties.Settings.Default.TempDirectory);       //Do the drive-to-drive copying.
        }

        /// <summary>
        /// Checked by the UI to know when the CloneToDrive command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// CloneToDrive shouldn't be possible if either WbfsDrive instance is busy, either drive is not loaded, or there are no items on the source drive.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void CloneToDriveCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy || wbfsDriveSecondary.Busy || wbfsDriveSecondary.LoadedDriveLetter.Equals(String.Empty) || wbfsDriveCurrent.LoadedDriveLetter.Equals(String.Empty) || EntryListBox.Items.Count == 0)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// DriveToDriveCopy command was executed. Check and make sure there are atually items selected to copy, there is a secondary drive loaded, 
        /// and theres enough temporary space. Then call DriveToDriveCopy to do the actual copying of the selected entries on the source drive, using the TempDirectory setting.
        /// </summary>
        private void DriveToDriveCopyCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (EntryListBox.SelectedIndex == -1 || EntryListBox.Items.Count == 0)          //No items in the Entries on Drive list or no items selected.
            {                                                                               //Show an error message
                MessageBox.Show(this, Properties.Resources.ErrorNoEntriesOnSourceNonSelectedStr, Properties.Resources.ErrorExtractingNoEntriesShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (wbfsDriveSecondary.LoadedDriveLetter.Equals(String.Empty))            //Check if the drive has been loaded yet, if not show an error message.
            {
                MessageBox.Show(this, Properties.Resources.ErrorNoSecondaryDriveLoadedStr, Properties.Resources.NoDriveSelectedShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!Utils.CheckTempForSpace(wbfsDriveCurrent.TempDirectory))
            {
                MessageBox.Show(this, Properties.Resources.ErrorNotEnoughSpaceTempFolderStr, Properties.Resources.ErrorNotEnoughSpaceShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            List<WbfsEntry> entriesToCopy = new List<WbfsEntry>();
            foreach (WbfsEntry item in EntryListBox.SelectedItems)
            {
                entriesToCopy.Add(item);
            }
            DriveToDriveCopy(entriesToCopy, Properties.Settings.Default.TempDirectory);
        }

        /// <summary>
        /// Checked by the UI to know when the DriveToDriveCopy command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// DriveToDriveCopy shouldn't be possible if either WbfsDrive instance is busy, either drive is not loaded, there are no items on the source drive or there are no items selected.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void DriveToDriveCopyCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy || wbfsDriveSecondary.Busy || wbfsDriveSecondary.LoadedDriveLetter.Equals(String.Empty) || wbfsDriveCurrent.LoadedDriveLetter.Equals(String.Empty) || EntryListBox.SelectedItems.Count == 0 || EntryListBox.Items.Count == 0)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// RefreshDriveLists command was executed. Rechecks the list of available drives, clears the comboboxes and adds the new list in by calling SyncDriveComboBoxes.
        /// </summary>
        private void RefreshDriveListsCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SyncDriveComboBoxes();
        }

        /// <summary>
        /// Checked by the UI to know when the RefreshDriveLists command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// RefreshDriveLists should always be possible except if one of the WbfsDrive instance are busy
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void RefreshDriveListsCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy || wbfsDriveSecondary.Busy)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Unloads the secondary drive and resets the UI for it.
        /// </summary>
        private void UnloadSecondaryDriveCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Reset the secondary drives values. Don't create a new instance of WbfsDrive, UI will not be bound correctly if wbfsDriveSecondary is reassigned.
            wbfsDriveSecondary.LoadedDriveLetter = String.Empty;
            wbfsDriveSecondary.EntriesOnDrive.Clear();
            wbfsDriveSecondary.DriveStats.SetNewStats(0, 0, 0, 0);
            SecondaryDriveMainGrid.IsEnabled = false;
            SyncDriveComboBoxes();
        }

        /// <summary>
        /// Checked by the UI to know when the UnloadSecondaryDrive command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// UnloadSecondaryDrive should always be possible except if one of the WbfsDrive instance are busy or theres no loaded drive.
        /// </summary>
        /// <param name="e">e.CanExecute: True if the command can be executed, false otherwise.</param>
        private void UnloadSecondaryDriveCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy || wbfsDriveSecondary.Busy || wbfsDriveSecondary.LoadedDriveLetter.Equals(String.Empty))
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// CreateChannel command was executed. Check if the number of items is 1 or if its more. If it's 1, show the disc ID name and target path dialog (ChannelCreatorDialog). Then call CreateChannel.
        /// Otherwise, show a folder browser dialog for selecting the output folder, call CreateChannel asynchronously to do the list of items that were selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateChannelCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Check that theres actually an item or more in the list and that at least one has been selected.
            if (wbfsDriveCurrent.EntriesToAdd.Count == 0 || AddListBox.SelectedIndex < 0 || AddListBox.SelectedItems.Count <= 0)
                return;
            //Check if channel creation is enabled, and whether the Channel creation settings are properly set (Source WAD path, Loader file path,Loader's placeholder ID and the location of the common key.
            if (!Properties.Settings.Default.EnableChannelCreation || Properties.Settings.Default.SourceWadPath.Trim().Length == 0 || Properties.Settings.Default.LoaderFilePath.Trim().Length == 0 || Properties.Settings.Default.LoaderOriginalTitleId.Trim().Length == 0 || Properties.Settings.Default.CommonKeyPath.Trim().Length == 0)
            {
                MessageBox.Show(this, Properties.Resources.ErrorInvalidChannelSettingsFixStr, Properties.Resources.ErrorInvalidSettingsStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (AddListBox.SelectedItems.Count == 1)        //If only one item is selected, show the ChannelCreatorDialog and synchronously call CreateChannel.
            {   //generate the channel ID using the usual method, to give the user a default value.
                string generatedChanId = LEADINGCHANLETTER + ((WbfsEntry)AddListBox.SelectedItem).EntryID.Substring(1, 3);
                ChannelCreatorDialog ccd = new ChannelCreatorDialog(generatedChanId) { Owner = this };       //Create the dialog and pass the generated ID
                if (!ccd.ShowDialog().GetValueOrDefault(false))                 //Show the dialog modally and default the result to false (cancel).
                    return;
                if (ccd.OutputChannelId.Trim().Length != 4 || ccd.OutputWadPath.Trim().Length == 0)
                {
                    MessageBox.Show(this, Properties.Resources.ErrorInvalidChanIdOrOutFileStr, Properties.Resources.ErrorInvalidValsShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                //If the user didn't cancel, check and make sure the extension ends in .wad (if the user typed the whole path, this may be the case). If not, add the extension.
                if (!Path.GetExtension(ccd.OutputWadPath).Equals(".wad"))
                    ccd.OutputWadPath = ccd.OutputWadPath + ".wad";
                List<ErrorDetails> errorList = CreateChannel(AddListBox.SelectedItems, ccd.OutputWadPath, ccd.OutputChannelId, null);       //Call the create channel method, passing the list of selected items (which only has one element), the output path and chan ID and null as the background worker.
                if (errorList != null && errorList.Count != 0)          //If there were errors show the error message
                {
                    MessageBox.Show(this, errorList[0].LongMessage, errorList[0].ShortMessage, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else                                                    //otherwise show a success message.
                {
                    MessageBox.Show(this, Properties.Resources.CompletedChannelCreationSuccessStr, Properties.Resources.CompletedChanCreationSuccessShortStr, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            else
            {
                //Show a folder browser dialog to allow the user to select the ouput folder for the created channel WADs
                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog { Description = Properties.Resources.SelectWadOutputFolderStr, ShowNewFolderButton = true };
                if (fbd.ShowDialog() != System.Windows.Forms.DialogResult.OK)   //Show the dialog modally, and only go beyond this point if the user clickced OK.
                    return;

                BackgroundWorker worker = new BackgroundWorker();               //Create a background worker to do the operation, hooking up the related events.
                worker.DoWork += new DoWorkEventHandler(createChannelWorker_DoWork);
                worker.ProgressChanged += new ProgressChangedEventHandler(createChannelWorker_ProgressChanged);
                worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(createChannelWorker_RunWorkerCompleted);
                worker.WorkerReportsProgress = true;                            //Allow progress reporting
                worker.WorkerSupportsCancellation = true;                       //Allow cancellation
                ArrayList argList = new ArrayList();
                argList.Add(AddListBox.SelectedItems);                          //Add the required arguments for the background worker to an argument list
                argList.Add(fbd.SelectedPath);                                  //The first argument was the list of selected items, the second is the selected output folder
                argList.Add(worker);                                            //and the third is the background woker (needed for progress reporting).
                worker.RunWorkerAsync(argList);                                 //Call the background worker to do CreateChannel asynchronously.

                pd = new ProgressDialog(Properties.Resources.CreatingChannelsProgressText, false, true, AddListBox.SelectedItems.Count, true, worker);     //Create a progress dialog with overall progress enabled and the total number of items set to the number of selected items
                pd.Owner = this;                                                                           //Set the owner of the dialog to this window to keep it centered
                pd.ShowDialog();                                                                            //Show the dialog modally to keep the user from doing anything else with the program while extracting
            }
        }

        /// <summary>
        /// Checked by the UI to know when the CreateChannel command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// CreateChannel should not be possible if the current WbfsDrive instance is busy or channel creation is disabled, or there are no entries in the list of Games to Add.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateChannelCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (wbfsDriveCurrent.Busy)
            {
                e.CanExecute = false;
                return;
            }
            if (!Properties.Settings.Default.EnableChannelCreation || wbfsDriveCurrent.EntriesToAdd.Count == 0 || AddListBox.SelectedIndex < 0 || AddListBox.SelectedItems.Count <= 0)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }

        /// <summary>
        /// Show the Help viewer whenever the Help menu item is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HelpCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            HelpViewer helpViewer = new HelpViewer();
            helpViewer.Show();          //Show the help viewer non-modally, allowing the user to continue interacting with the applicaiton
        }

        /// <summary>
        /// Checked by the UI to know when the HelpCommand command is possible. Automatically disables buttons and menu items
        /// bound to this command, if e.CanExecute is false.
        /// HelpCommand can always be executed since the Help viewer is shown non-modally.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HelpCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        #endregion Command Event Handlers

        private void TestButton_Click(object sender, RoutedEventArgs rea)
        {
            ////libwbfsNET.wbfs_t wbfst = libwbfsNET.WbfsNET.TryOpen(null, "I", 0);
            //libwbfsNET.WbfsIntermWrapper.OpenDrive("I");
            //uint blocks = 0;
            //float total = -1, used = -1, free = -1;
            //int x=libwbfsNET.WbfsIntermWrapper.GetDriveStats(ref blocks, ref total, ref used, ref free);
            //StringBuilder discId = new StringBuilder();
            //StringBuilder discName = new StringBuilder(256);
            //float size = 0;
            //int result = libwbfsNET.WbfsIntermWrapper.GetDiscInfo(0, discId, ref size, discName);
            //return;
            //wbfsDriveCurrent.LoadedDriveLetter = "S";
            //WbfsIntermWrapper.OpenDrive("S");
            //WbfsIntermWrapper.CloseDrive();
            //ReloadDrive();
            //WBFSEntry temp = (WBFSEntry)AddListBox.SelectedItem;
            ////CreateHbcEntry(temp, @"E:\temp\");
            //temp.CopiedState = WBFSEntry.CopiedStates.Failed;
            //temp.ErrorMessageToolTip = "Testing the error messages";

            //List<String> xmlResult = ParseDolMetadataXml(@"E:\temp\Alpha version\metadata.xml");


            //if (AddListBox.SelectedIndex == -1 || AddListBox.SelectedItems.Count > 1 || AddListBox.SelectedItem == null)
            //    return;
            //ChannelCreatorDialog ccd = new ChannelCreatorDialog();
            //string outfolder = Path.Combine(Properties.Settings.Default.TempDirectory, "ChanTemp");
            //try
            //{
            //    if (!ccd.ShowDialog().GetValueOrDefault(false))
            //        return;
            //    if (/*ccd.IsoFileTextBox.Text.Length == 0 || ccd.SrcWadFileTextBox.Text.Length == 0 || ccd.DolFileTextBox.Text.Length == 0 || ccd.origTitIDTextBox.Text.Length == 0 ||*/
            //        /* ccd.keyFileTextBox.Text.Length == 0 || */
            //       Properties.Settings.Default.SourceWadPath.Trim().Length == 0 || Properties.Settings.Default.LoaderFilePath.Trim().Length == 0 || Properties.Settings.Default.LoaderOriginalTitleId.Trim().Length == 0 || Properties.Settings.Default.CommonKeyPath.Trim().Length == 0 ||
            //       ccd.ChanIDTextBox.Text.Length == 0 || ccd.OutWadTextBox.Text.Length == 0)
            //    {
            //        MessageBox.Show(this, "Missing info. " + Properties.Settings.Default.SourceWadPath + " " + Properties.Settings.Default.LoaderFilePath + " " + Properties.Settings.Default.LoaderOriginalTitleId + " " + Properties.Settings.Default.CommonKeyPath + " " + ccd.ChanIDTextBox.Text + " " + ccd.OutWadTextBox.Text);
            //        return;
            //    }
            //    if (!Directory.Exists(outfolder))
            //        Directory.CreateDirectory(outfolder);
            //    if (!((WbfsEntry)AddListBox.SelectedItem).IsFilePathValid())
            //    {
            //        MessageBox.Show(this, "Invalid iso path.");
            //        return;
            //    }
            //    //int result = ChannelCreationWrapper.ChannelCreationWrapper.CreateChannel(((WbfsEntry)AddListBox.SelectedItem).FilePath,//ccd.IsoFileTextBox.Text,
            //    //    ccd.SrcWadFileTextBox.Text, ccd.DolFileTextBox.Text, ccd.origTitIDTextBox.Text, ccd.keyFileTextBox.Text, ((WbfsEntry)AddListBox.SelectedItem).EntryID, ccd.ChanIDTextBox.Text, outfolder, ccd.OutWadTextBox.Text);
            //    int result = ChannelCreationWrapper.ChannelCreationWrapper.CreateChannel(((WbfsEntry)AddListBox.SelectedItem).FilePath, Properties.Settings.Default.SourceWadPath, Properties.Settings.Default.LoaderFilePath, Properties.Settings.Default.LoaderOriginalTitleId, Properties.Settings.Default.CommonKeyPath, ((WbfsEntry)AddListBox.SelectedItem).EntryID, ccd.ChanIDTextBox.Text, outfolder, ccd.OutWadTextBox.Text);
            //    MessageBox.Show(result.ToString());
            //}
            //catch (Exception e)
            //{
            //    try
            //    {
            //        System.IO.StreamWriter sw = new System.IO.StreamWriter(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "log.txt"), true);
            //        if (e.InnerException != null)
            //        {
            //            sw.WriteLine("Fatal error occurred. Please send the following information to the application developer at wbfsmanager@gmail.com (You can copy this text by pressing Ctrl+C): " +
            //                 "Version 2.5, Message: " + e.Message + " \nInnerException: " + e.InnerException.Message
            //                     + " \nStack trace: " + e.StackTrace + "\nDetails: " + ((WbfsEntry)AddListBox.SelectedItem).FilePath + " " + Properties.Settings.Default.SourceWadPath + " " + Properties.Settings.Default.LoaderFilePath + " " + Properties.Settings.Default.LoaderOriginalTitleId + " " + Properties.Settings.Default.CommonKeyPath + " " + ((WbfsEntry)AddListBox.SelectedItem).EntryID + " " + ccd.ChanIDTextBox.Text + " " + outfolder + " " + ccd.OutWadTextBox.Text);
            //        }
            //        else
            //        {
            //            sw.WriteLine("Fatal error occurred. Please send the following information to the application developer at wbfsmanager@gmail.com (You can copy this text by pressing Ctrl+C): " +
            //              "Version 2.5, Message: " + e.Message
            //                  + " \nStack trace: " + e.StackTrace + "\nDetails: " + ((WbfsEntry)AddListBox.SelectedItem).FilePath + " " + Properties.Settings.Default.SourceWadPath + " " + Properties.Settings.Default.LoaderFilePath + " " + Properties.Settings.Default.LoaderOriginalTitleId + " " + Properties.Settings.Default.CommonKeyPath + " " + ((WbfsEntry)AddListBox.SelectedItem).EntryID + " " + ccd.ChanIDTextBox.Text + " " + outfolder + " " + ccd.OutWadTextBox.Text);
            //        }
            //        sw.Flush();
            //        sw.Close();
            //    }
            //    catch
            //    {
            //    }
            //    if (e.GetType() == typeof(BadImageFormatException))
            //    {
            //        MessageBox.Show("Fatal error occurred. You are using the wrong version of this software for your operating system. If you are using Windows 64-bit, please download the 64-bit version of WBFS Manager.", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }
            //    else if (e.InnerException != null)
            //    {
            //        MessageBox.Show("Fatal error occurred. Please send the following information to the application developer at wbfsmanager@gmail.com (You can copy this text by pressing Ctrl+C or use the log file in the installation directory (must Run as Administrator in Vista+)): " +
            //          "Version 2.5, Message: " + e.Message + " \nInnerException: " + e.InnerException.Message
            //              + " \nStack trace: " + e.StackTrace + "\nDetails: " + ((WbfsEntry)AddListBox.SelectedItem).FilePath + " " + Properties.Settings.Default.SourceWadPath + " " + Properties.Settings.Default.LoaderFilePath + " " + Properties.Settings.Default.LoaderOriginalTitleId + " " + Properties.Settings.Default.CommonKeyPath + " " + ((WbfsEntry)AddListBox.SelectedItem).EntryID + " " + ccd.ChanIDTextBox.Text + " " + outfolder + " " + ccd.OutWadTextBox.Text, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }
            //    else
            //    {
            //        MessageBox.Show("Fatal error occurred. Please send the following information to the application developer at wbfsmanager@gmail.com (You can copy this text by pressing Ctrl+C or use the log file in the installation directory (must Run as Administrator in Vista+)): " +
            //          "Version 2.5, Message: " + e.Message
            //              + " \nStack trace: " + e.StackTrace + "\nDetails: " + ((WbfsEntry)AddListBox.SelectedItem).FilePath + " " + Properties.Settings.Default.SourceWadPath + " " + Properties.Settings.Default.LoaderFilePath + " " + Properties.Settings.Default.LoaderOriginalTitleId + " " + Properties.Settings.Default.CommonKeyPath + " " + ((WbfsEntry)AddListBox.SelectedItem).EntryID + " " + ccd.ChanIDTextBox.Text + " " + outfolder + " " + ccd.OutWadTextBox.Text, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }
            //}
            //if (Directory.Exists(outfolder))
            //    Directory.Delete(outfolder, true);




            //int result = ChannelCreationWrapper.ChannelCreationWrapper.CreateChannel(@"E:\fatal-swtfu\fatal-swtfu.iso",
            //    @"E:\temp\Channel creation\MyTest\in.wad", @"E:\temp\Channel creation\MyTest\yal.dol", @"RCPP18", @"E:\temp\Channel creation\ManualTest\common-key.bin", @"RSTE64", "WB01", @"E:\temp\Channel creation\WBFSTest", @"E:\temp\Channel creation\WBFSTest\out.wad");
            //WbfsIntermWrapper.OpenDrive("I");

        }

    }
}