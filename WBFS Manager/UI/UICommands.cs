using System.Windows.Input;

namespace WBFSManager.UI.Commands
{
    /// <summary>
    /// The list of UI commands possible. Used by WPF to unify UI actions and automatically disable commands that aren't allowed at a given time.
    /// </summary>
    public class UICommands
    {
        #region Constructor
        static UICommands()
        {
            InputGestureCollection inputKeys;

            inputKeys = new InputGestureCollection {new KeyGesture(Key.L, ModifierKeys.Control, "Ctrl+L")};
            _loadDrive = new RoutedUICommand("Load Drive", "LoadDrive", typeof(UICommands), inputKeys);

            _loadSecondaryDrive = new RoutedUICommand("Load Secondary Drive", "LoadSecondaryDrive", typeof(UICommands));
            _unloadSecondaryDrive = new RoutedUICommand("Unload Secondary Drive", "UnloadSecondaryDrive", typeof(UICommands));

            inputKeys = new InputGestureCollection {new KeyGesture(Key.Q, ModifierKeys.Control, "Ctrl+Q"), new KeyGesture(Key.X, ModifierKeys.Alt, "Alt+X")};
            _exit = new RoutedUICommand("Exit", "Exit", typeof(UICommands), inputKeys);

            inputKeys = new InputGestureCollection {new KeyGesture(Key.E, ModifierKeys.Control, "Ctrl+E")};
            _extract = new RoutedUICommand("Extract ISO...", "Extract", typeof(UICommands), inputKeys);

            inputKeys = new InputGestureCollection {new KeyGesture(Key.A, ModifierKeys.Control, "Ctrl+A")};
            _addToDrive = new RoutedUICommand("Add to Drive", "Add", typeof(UICommands), inputKeys);

            inputKeys = new InputGestureCollection {new KeyGesture(Key.D, ModifierKeys.Control, "Ctrl+D")};
            _removeFromDrive = new RoutedUICommand("Delete", "Delete", typeof(UICommands), inputKeys);

            inputKeys = new InputGestureCollection {new KeyGesture(Key.R, ModifierKeys.Control, "Ctrl+R")};
            _removeFromList = new RoutedUICommand("Remove From List", "Remove", typeof(UICommands), inputKeys);

            inputKeys = new InputGestureCollection {new KeyGesture(Key.B, ModifierKeys.Control, "Ctrl+B")};
            _browse = new RoutedUICommand("Browse...", "Browse", typeof(UICommands), inputKeys);

            inputKeys = new InputGestureCollection {new KeyGesture(Key.C, ModifierKeys.Alt, "Alt+C")};
            _clearList = new RoutedUICommand("Clear List", "Clear", typeof(UICommands), inputKeys);

            _format = new RoutedUICommand("Format", "Format", typeof(UICommands));

            _renameOnDrive = new RoutedUICommand("Rename", "RenameOnDrive", typeof(UICommands));

            _renameOnAdd = new RoutedUICommand("Rename", "RenameOnAdd", typeof(UICommands));

            _sortByName = new RoutedUICommand("Sort By Name", "SortByName", typeof(UICommands));
            _sortById = new RoutedUICommand("Sort By ID", "SortById", typeof(UICommands));
            _sortBySize = new RoutedUICommand("Sort By Size", "SortBySize", typeof(UICommands));
            _sortByStatus = new RoutedUICommand("Sort By Status", "SortByStatus", typeof(UICommands));
            _sortByIndex = new RoutedUICommand("Sort By Index", "SortByIndex", typeof(UICommands));
            _sortByFilePath = new RoutedUICommand("Sort By Path", "SortByFilePath", typeof(UICommands));

            _setCoversDir = new RoutedUICommand("Set Covers Directory...", "SetCoversDir", typeof(UICommands));
            _showCovers = new RoutedUICommand("Show Covers", "ShowCovers", typeof(UICommands));
            _showOptions = new RoutedUICommand("Show Options", "ShowOptions", typeof(UICommands));

            _makeSingleHBC = new RoutedUICommand("Make HBC Entry", "MakeSingleHBC", typeof(UICommands));
            _makeAllHBC = new RoutedUICommand("Make HBC Entries for All", "MakeAllHBC", typeof(UICommands));

            _createChannel = new RoutedUICommand("Create Channel(s)", "CreateChannel", typeof(UICommands));

            _changeLanguage = new RoutedUICommand("Language", "ChangeLanguage", typeof(UICommands));

            _checkForUpdates = new RoutedUICommand("Check for Updates", "CheckForUpdates", typeof(UICommands));

            _downloadCoversFromWeb = new RoutedUICommand("Download Covers from Web", "DownloadCoversFromWeb", typeof(UICommands));

            _exportListToFile = new RoutedUICommand("Export List to File", "ExportListToFile", typeof(UICommands));

            _refreshDriveLists = new RoutedUICommand("Refresh Drive List", "RefreshDriveLists", typeof(UICommands));

            _cloneToDrive = new RoutedUICommand("Clone", "CloneToDrive", typeof(UICommands));
            _driveToDriveCopy = new RoutedUICommand("Drive-To-Drive Copy", "DriveToDriveCopy", typeof(UICommands));

            inputKeys = new InputGestureCollection {new KeyGesture(Key.A, ModifierKeys.Alt, "Alt+A")};
            _about = new RoutedUICommand("About...", "About", typeof(UICommands), inputKeys);
        }
        #endregion
        #region Fields
        private static RoutedUICommand _removeFromDrive;

        private static RoutedUICommand _loadDrive;

        private static RoutedUICommand _loadSecondaryDrive;

        private static RoutedUICommand _unloadSecondaryDrive;

        private static RoutedUICommand _exit;

        private static RoutedUICommand _extract;

        private static RoutedUICommand _addToDrive;

        private static RoutedUICommand _removeFromList;

        private static RoutedUICommand _browse;

        private static RoutedUICommand _clearList;

        private static RoutedUICommand _format;

        private static RoutedUICommand _renameOnDrive;

        private static RoutedUICommand _renameOnAdd;

        private static RoutedUICommand _sortByName;

        private static RoutedUICommand _sortById;

        private static RoutedUICommand _sortBySize;

        private static RoutedUICommand _sortByStatus;

        private static RoutedUICommand _sortByIndex;

        private static RoutedUICommand _sortByFilePath;

        private static RoutedUICommand _setCoversDir;

        private static RoutedUICommand _showCovers;

        private static RoutedUICommand _showOptions;

        private static RoutedUICommand _makeSingleHBC;

        private static RoutedUICommand _makeAllHBC;

        private static RoutedUICommand _createChannel;

        private static RoutedUICommand _changeLanguage;

        private static RoutedUICommand _checkForUpdates;

        private static RoutedUICommand _downloadCoversFromWeb;

        private static RoutedUICommand _exportListToFile;

        private static RoutedUICommand _refreshDriveLists;

        private static RoutedUICommand _cloneToDrive;

        private static RoutedUICommand _driveToDriveCopy;
        
        private static RoutedUICommand _about;
        #endregion
        #region Properties
        public static RoutedUICommand RemoveFromDrive
        {
            get
            {
                return _removeFromDrive;
            }
        }

        public static RoutedUICommand LoadDrive
        {
            get
            {
                return _loadDrive;
            }
        }

        public static RoutedUICommand LoadSecondaryDrive
        {
            get
            {
                return _loadSecondaryDrive;
            }
        }

        public static RoutedUICommand UnloadSecondaryDrive
        {
            get
            {
                return _unloadSecondaryDrive;
            }
        }

        public static RoutedUICommand Exit
        {
            get
            {
                return _exit;
            }
        }

        public static RoutedUICommand Extract
        {
            get
            {
                return _extract;
            }
        }

        public static RoutedUICommand AddToDrive
        {
            get
            {
                return _addToDrive;
            }
        }

        public static RoutedUICommand RemoveFromList
        {
            get
            {
                return _removeFromList;
            }
        }

        public static RoutedUICommand Browse
        {
            get
            {
                return _browse;
            }
        }

        public static RoutedUICommand ClearList
        {
            get
            {
                return _clearList;
            }
        }

        public static RoutedUICommand Format
        {
            get
            {
                return _format;
            }
        }

        public static RoutedUICommand RenameOnDrive
        {
            get
            {
                return _renameOnDrive;
            }
        }

        public static RoutedUICommand RenameOnAdd
        {
            get
            {
                return _renameOnAdd;
            }
        }

        public static RoutedUICommand SortByName
        {
            get
            {
                return _sortByName;
            }
        }

        public static RoutedUICommand SortById
        {
            get
            {
                return _sortById;
            }
        }

        public static RoutedUICommand SortBySize
        {
            get
            {
                return _sortBySize;
            }
        }

        public static RoutedUICommand SortByStatus
        {
            get
            {
                return _sortByStatus;
            }
        }

        public static RoutedUICommand SortByIndex
        {
            get
            {
                return _sortByIndex;
            }
        }

        public static RoutedUICommand SortByFilePath
        {
            get
            {
                return _sortByFilePath;
            }
        }

        public static RoutedUICommand SetCoversDir
        {
            get
            {
                return _setCoversDir;
            }
        }

        public static RoutedUICommand ShowCovers
        {
            get
            {
                return _showCovers;
            }
        }

        public static RoutedUICommand ShowOptions
        {
            get
            {
                return _showOptions;
            }
        }

        public static RoutedUICommand MakeSingleHBC
        {
            get
            {
                return _makeSingleHBC;
            }
        }

        public static RoutedUICommand MakeAllHBC
        {
            get
            {
                return _makeAllHBC;
            }
        }

        public static RoutedUICommand CreateChannel
        {
            get
            {
                return _createChannel;
            }
        }

        public static RoutedUICommand ChangeLanguage
        {
            get
            {
                return _changeLanguage;
            }
        }

        public static RoutedUICommand CheckForUpdates
        {
            get
            {
                return _checkForUpdates;
            }
        }

        public static RoutedUICommand DownloadCoversFromWeb
        {
            get
            {
                return _downloadCoversFromWeb;
            }
        }
        public static RoutedUICommand ExportListToFile
        {
            get
            {
                return _exportListToFile;
            }
        }
        public static RoutedUICommand RefreshDriveLists
        {
            get
            {
                return _refreshDriveLists;
            }
        }
        public static RoutedUICommand CloneToDrive
        {
            get
            {
                return _cloneToDrive;
            }
        }

        public static RoutedUICommand DriveToDriveCopy
        {
            get
            {
                return _driveToDriveCopy;
            }
        }
        public static RoutedUICommand About
        {
            get
            {
                return _about;
            }
        }
        #endregion
    }
}
