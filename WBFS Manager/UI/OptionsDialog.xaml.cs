using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WinForms=System.Windows.Forms;
using System.IO;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Win32;
using WBFSManager.Data;
using System.Reflection;
using System.Xml.Linq;

namespace WBFSManager.UI
{
    /// <summary>
    /// Interaction logic for OptionsDialog.xaml
    /// </summary>
    public partial class OptionsDialog : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Property representing the option to show covers or not. Bound to UI, two-way.
        /// </summary>
        public bool ShowCovers { get; set; }
        /// <summary>
        /// Property representing the option to download covers from the web or not. Bound to UI, two-way.
        /// </summary>
        public bool DownloadFromWeb { get; set; }
        /// <summary>
        /// Property represting the path to use as the temporary directory (for indirect drive-to-drive and RAR extraction). Bound to UI, two-way.
        /// </summary>
        public String TempDirectory { get; set; }
        /// <summary>
        /// Property represting the list of directories to search for covers in. Bound to UI, one-way to destination (default).
        /// </summary>
        public ObservableCollection<String> CoverDirs { get; set; }
        /// <summary>
        /// Private lock flag, used to prevent the user from closing the options menu (cancelling out) on first run without selecting a temp directory.
        /// </summary>
        private bool _allowClosing = true;
        /// <summary>
        /// Property representing the option to enable RAR extraction. Bound to UI, two-way.
        /// </summary>
        public bool EnableRarExtraction { get; set; }
        /// <summary>
        /// Property representing the option to enable automatic update checks. Bound to UI, two-way.
        /// </summary>
        public bool AutomaticUpdateChecks { get; set; }

        public String SrcWadFile { get; set; }

        public String KeyFile { get; set; }

        public String LoaderFile { get; set; }

        public String OriginalTitleId { get; set; }

        public bool EnableChannelCreation { get; set; }

        public libwbfsNET.WbfsIntermWrapper.PartitionSelector PartitionToUse { get; set; }

        public ObservableCollection<ChannelPreset> _presets = new ObservableCollection<ChannelPreset>();


        /// <summary>
        /// Parameterized constructor for the options dialog which sets the values of the different options to the values passed in as parameters.
        /// </summary>
        /// <param name="automaticUpdateChecks"></param>
        /// <param name="firstRun">A flag indicating whether this is the first run (if so closing/cancelling out is not possible)</param>
        /// <param name="showCovers">Whether or not showing covers is enabled.</param>
        /// <param name="downloadFromWeb">Whether or not downloading covers from the web is enabled.</param>
        /// <param name="tempFolder">The temporary folder for RAR extraction and indirect drive-to-drive copying.</param>
        /// <param name="coverDirs">The list of user selected cover directories.</param>
        /// <param name="enableRarExtraction">Whether or not RAR extraction is enabled.</param>
        /// <param name="srcWadFile">The path to the source (base) WAD file.</param>
        /// <param name="enableChannelCreation">Whether or not to enable channel creation.</param>
        /// <param name="partitionToUse">The settings for Wii disc partitions to save when adding games to the WBFS drive.</param>
        /// <param name="keyFile">The path to the common-key.bin file</param>
        /// <param name="loaderFile">The path to the loader .dol file.</param>
        /// <param name="originalTitleId">The base WAD file's placeholder title ID.</param>
        public OptionsDialog(bool firstRun, bool showCovers, bool downloadFromWeb, String tempFolder, StringCollection coverDirs, bool enableRarExtraction, bool automaticUpdateChecks, String srcWadFile, String keyFile, String loaderFile, String originalTitleId, bool enableChannelCreation, libwbfsNET.WbfsIntermWrapper.PartitionSelector partitionToUse)
        {
            ShowCovers = showCovers;
            DownloadFromWeb = downloadFromWeb;
            TempDirectory = tempFolder;                                 //Initialize all these values before calling initialize component otherwise values will be null causing wierd UI acting up.
            CoverDirs = new ObservableCollection<String>();
            EnableRarExtraction = enableRarExtraction;
            AutomaticUpdateChecks = automaticUpdateChecks;
            if (coverDirs != null)
            {
                foreach (String item in coverDirs)
                {
                    CoverDirs.Add(item);
                }
            }
            SrcWadFile = srcWadFile;
            KeyFile = keyFile;
            LoaderFile = loaderFile;
            OriginalTitleId = originalTitleId;
            EnableChannelCreation = enableChannelCreation;
            PartitionToUse = partitionToUse;
            InitializeComponent();                                      //Initialize the UI components
            if (firstRun)                                               //If this is the first run disable the cancel button and don't allow closing.
            {
                CancelButton.IsEnabled = false;
                _allowClosing = false;
            }
            LoadPartitionChoices(partitionToUse);
            LoadPresets();
            xmlPresetComboBox.ItemsSource = _presets;
            xmlPresetComboBox.DisplayMemberPath = "Description";
            if (firstRun && xmlPresetComboBox.Items.Count > 0)
            {
                xmlPresetComboBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Default constructor, better if first run uses this one, since it sets all the options to default values and the temp folder to the user's Winodws-based temp folder.
        /// </summary>
        /// <param name="firstRun">Whether or not this is the first run of the program.</param>
        public OptionsDialog(bool firstRun)
        {
            ShowCovers = true;
            DownloadFromWeb = true;                                             //Set everything to their default values, and load in the user's Windows-based temp folder as the default path for temp folder
            EnableRarExtraction = true;
            TempDirectory = Path.Combine(Path.GetTempPath(), "WBFSTemp");
            AutomaticUpdateChecks = true;
            CoverDirs = new ObservableCollection<String>();
            SrcWadFile = String.Empty;
            KeyFile = String.Empty;
            LoaderFile = String.Empty;
            OriginalTitleId = String.Empty;
            EnableChannelCreation = false;
            PartitionToUse = libwbfsNET.WbfsIntermWrapper.PartitionSelector.ONLY_GAME_PARTITION;
            InitializeComponent();
            if (firstRun)
            {
                CancelButton.IsEnabled = false;
                _allowClosing = false;
            }
            LoadPartitionChoices(libwbfsNET.WbfsIntermWrapper.PartitionSelector.ONLY_GAME_PARTITION);
            LoadPresets();
            xmlPresetComboBox.ItemsSource = _presets;
            xmlPresetComboBox.DisplayMemberPath = "Description";
            if (firstRun && xmlPresetComboBox.Items.Count > 0)
            {
                xmlPresetComboBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Event handler for the browse button for selecting the temp folder.
        /// </summary>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            WinForms.FolderBrowserDialog fbd = new WinForms.FolderBrowserDialog {Description = Properties.Resources.ChooseTempFolderText, ShowNewFolderButton = true};      //Show a folder browser and allow the folder to be created or selected.
            if (fbd.ShowDialog() != WinForms.DialogResult.OK)                           //if the users cancels out, stop and return
                return;
            if (!Utils.CheckTempForSpace(fbd.SelectedPath))                             //Otherwise check to see if theres enough space on the specified drive.
            {                                                                           //If not, show an error message and stop and return.
                MessageBox.Show(this, Properties.Resources.ErrorNotEnoughSpaceTempFolderStr, Properties.Resources.ErrorNotEnoughSpaceShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            TempDirectory = fbd.SelectedPath;                                           //Otherwise set the temp directory property to the path selected in the folder browser dialog
            NotifyPropertyChanged("TempDirectory");                                     //Tell the UI to check the TempDirectory property so as to reflect the value on the UI
        }

        /// <summary>
        /// Event handler for when the add button is clicked to add a directory to the list of cover directories.
        /// </summary>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            WinForms.FolderBrowserDialog fbd = new WinForms.FolderBrowserDialog {Description = Properties.Resources.SelectDirForHBCStr, ShowNewFolderButton = true};      //Show a folder browser and allow the folder to be created or selected.

            if (fbd.ShowDialog() != WinForms.DialogResult.OK)       // Show the folder browser dilaog asking the user for the cover directory they'd like
            {                                                                   //If the result is anything but OK, cancel out and return.
                return;
            }

            string dir_path = fbd.SelectedPath.Trim();
            if (string.IsNullOrEmpty(dir_path))                       //Make sure the selected directory name isn't empty.
            {
                MessageBox.Show(this, Properties.Resources.MustSelectExistingFolderStr, Properties.Resources.MustSelectExistingFolderShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            CoverDirs.Add(fbd.SelectedPath);                                    //Add it to the list of cover directories proeprty
        }

        /// <summary>
        /// Event handler for when the remove button is clicked to remove a directory from the list of cover directories.
        /// </summary>
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            //Make sure theeres actually something in the list and that theres at least one selected item
            if (CoverDirsListBox.Items.Count < 0 || CoverDirsListBox.SelectedIndex == -1 || CoverDirsListBox.SelectedItems.Count < 0)
                return;         // if not, dont do anything.
            List<String> toDelete = new List<String>();                         //otherwise make a copy of the ones to delete, then delete them from the main list
            foreach (String item in CoverDirsListBox.SelectedItems)             //have to make a copy first due to the foreach loop as well as the fact that the UI is monitoring th list.
            {
                toDelete.Add(item);
            }
            foreach (String item in toDelete)
            {
                CoverDirs.Remove(item);
            }
        }

        /// <summary>
        /// Event handler for when the clear button is clicked to clear the list of cover directories.
        /// </summary>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            CoverDirs.Clear();
        }


        /// <summary>
        /// Event handler for when the OK button is clicked. Makes a final check on the temp directory and gives a warning if theres no space or cancels the ok if no value has been entered for it.
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            //Make sure the directory that's been entered is not an empty string and that it actually exists. If not, show an error message and stay on the options dialog.
            if (TempDirectory.Length == 0)
            {
                MessageBox.Show(this, Properties.Resources.MustSelectValidFolderText, Properties.Resources.InvalidTempFolderStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!Directory.Exists(TempDirectory))
            {
                try
                {
                    Directory.CreateDirectory(TempDirectory);
                }
                catch
                {
                    MessageBox.Show(this, Properties.Resources.ErrorUnableToCreateTempDirStr + TempDirectory, Properties.Resources.ErrorUnableToCreateDirShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            //Make sure the drive the temp folder is on has enough space for at least a single layer DVD
            if (!Utils.CheckTempForSpace(TempDirectory))
            {
                // If there isnt enough space, show a warning and give the user the option to go ahead anyway.
                MessageBoxResult result = MessageBox.Show(this, Properties.Resources.WarningNotEnoughSpaceTempStr, Properties.Resources.ErrorNotEnoughSpaceShortStr, MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if(result!=MessageBoxResult.OK)     //If the user hits cancel, they've decided not to go ahead with the selected directory which doesnt have enough space.
                    return;
            }
            if (EnableChannelCreation && (SrcWadFile.Trim().Length == 0 || LoaderFile.Trim().Length == 0 || KeyFile.Trim().Length == 0 || OriginalTitleId.Trim().Length == 0))
            {
                MessageBox.Show(this, Properties.Resources.ErrorInvalidChanCreationValsStr, Properties.Resources.ErrorInvalidValsShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (PartitionsToUseComboBox.SelectedIndex < 0)
            {
                MessageBox.Show(this, Properties.Resources.ErrorInvalidPartitionSettingStr, Properties.Resources.ErrorInvalidValsShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (PartitionsToUseComboBox.SelectedIndex == 0)
            {
                PartitionToUse = libwbfsNET.WbfsIntermWrapper.PartitionSelector.ONLY_GAME_PARTITION;
            }
            else if (PartitionsToUseComboBox.SelectedIndex == 1)
            {
                PartitionToUse = libwbfsNET.WbfsIntermWrapper.PartitionSelector.REMOVE_UPDATE_PARTITION;
            }
            else
                PartitionToUse = libwbfsNET.WbfsIntermWrapper.PartitionSelector.ALL_PARTITIONS;

            _allowClosing = true;       //Allow closing now that everything checks out.
            DialogResult = true;        //Set the result of this dialog to true, meaning the OK button was clicked and was successful. This automatically closes the dialog since it's shown modally.
        }

        /// <summary>
        /// Closes the dialog and indicates that the user cancelled out, rejecting any changes made.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;       //If the cancel button was clicked, set the dialog result to false, meaning cancel. This automatically closes the dialog since it was shown modally.
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_allowClosing)         //If closing is not allowed, cancel the closing action and force the dialog to remain open until the user OKs (this is necessary for the first run.)
                e.Cancel = true;
        }

        #region INotifyPropertyChanged Members
        /// <summary>
        /// Tells the UI to check the named property for any changes and update the UI accordingly
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        #endregion

        private void WadBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog {Title = Properties.Resources.SelectBaseWadStr, Filter = "WAD file (*.wad)|*.wad", CheckFileExists = true};
            if (!ofd.ShowDialog().GetValueOrDefault(false))                           //if the users cancels out, stop and return
                return;
            if (!File.Exists(ofd.FileName))
            {
                MessageBox.Show(this, Properties.Resources.ErrorInvalidBaseWadFilenameStr, Properties.Resources.ErrorInvalidFilename, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            SrcWadFile = ofd.FileName;
            NotifyPropertyChanged("SrcWadFile");
        }

        private void KeyBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog {Title = Properties.Resources.SelectCommonKeyFileStr, Filter = "Common key file (*.bin)|*.bin|All files (*.*)|*.*", CheckFileExists = true};
            if (!ofd.ShowDialog().GetValueOrDefault(false))                           //if the users cancels out, stop and return
                return;
            if (!File.Exists(ofd.FileName))
            {
                MessageBox.Show(this, Properties.Resources.ErrorInvalidCommonKeyFilenameStr, Properties.Resources.ErrorInvalidFilename, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            KeyFile = ofd.FileName;
            NotifyPropertyChanged("KeyFile");
        }

        private void DolBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog {Title = Properties.Resources.SelectLoaderDolFileStr, Filter = "DOL file (*.dol)|*.dol", CheckFileExists = true};
            if (!ofd.ShowDialog().GetValueOrDefault(false))                           //if the users cancels out, stop and return
                return;
            if (!File.Exists(ofd.FileName))
            {
                MessageBox.Show(this, Properties.Resources.ErrorInvalidDolFilenameStr, Properties.Resources.ErrorInvalidFilename, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            LoaderFile = ofd.FileName;
            NotifyPropertyChanged("LoaderFile");
        }

        private void xmlPresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (xmlPresetComboBox.SelectedItem != null)
            {
                ChannelPreset temp = (ChannelPreset)xmlPresetComboBox.SelectedItem;
                DolFileTextBox.Text = temp.DolFilePath;
                LoaderFile = temp.DolFilePath;
                NotifyPropertyChanged("LoaderFile");
                origTitIDTextBox.Text = temp.OriginalTitleID;
                OriginalTitleId = temp.OriginalTitleID;
                NotifyPropertyChanged("OriginalTitleId");
            }
        }
        private void LoadPresets()
        {
            string[] xmlFiles = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Channels"), "*.xml", SearchOption.AllDirectories);
            foreach (string item in xmlFiles)
            {
                String xmlParentDirectory = Path.GetDirectoryName(item);
                XElement elem = XElement.Load(item);
                _presets.Add(new ChannelPreset(elem.Element(ChannelPreset.DESCRIPTIONELEMENTNAME).Value,
                    Path.Combine(xmlParentDirectory, elem.Element(ChannelPreset.DOLFILENAMEELEMENTNAME).Value),
                    elem.Element(ChannelPreset.ORIGINALTITLEIDELEMENTNAME).Value));
            }

        }

        private void LoadPartitionChoices(libwbfsNET.WbfsIntermWrapper.PartitionSelector partitionToUse)
        {
            PartitionsToUseComboBox.Items.Add(Properties.Resources.PartitonOptionOnlyGameStr);
            PartitionsToUseComboBox.Items.Add(Properties.Resources.ParitionOptionRemoveUpdateStr);
            PartitionsToUseComboBox.Items.Add(Properties.Resources.PartitionOptionAllPartStr);
            if (partitionToUse == libwbfsNET.WbfsIntermWrapper.PartitionSelector.ONLY_GAME_PARTITION)
                PartitionsToUseComboBox.SelectedIndex = 0;
            else if (partitionToUse == libwbfsNET.WbfsIntermWrapper.PartitionSelector.REMOVE_UPDATE_PARTITION)
                PartitionsToUseComboBox.SelectedIndex = 1;
            else
                PartitionsToUseComboBox.SelectedIndex = 2;
        }
    }
}
