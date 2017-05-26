using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace WBFSManager.Data
{
    public class WbfsEntry : INotifyPropertyChanged
    {
        #region Constants and Enums
        /// <summary>
        /// Indicates that the entry is related to an entry on the drive, as such it has tno file path on the user's computer.
        /// Since :ONDRIVE: isn't a valid Windows filename, it's difficult to mistake this for an actual file.
        /// </summary>
        public const string INVALIDFILE=":ONDRIVE:";
        /// <summary>
        /// Indicates that no cover was found for the entry.
        /// Since :NOCOVER: isn't a valid Windows filename, it's difficult to mistake this for an actual file.
        /// </summary>
        public const string NOCOVER = ":NOCOVER:";
        /// <summary>
        /// Indicates that the cover is still being downloaded (asynchronously) and that the UI should display the download in progress placeholder cover
        /// Since :COVERDOWNLOADING: isn't a valid Windows filename, it's difficult to mistake this for an actual file.
        /// </summary>
        public const string COVERDOWNLOADING = ":COVERDOWNLOADING:";
        /// <summary>
        /// The possible states for the entry in terms of whether it's been copied to the WBFS drive (or copied over to the secondary WBFS drive)
        /// NotYetCopied will result in no icon being displayed in the UI next to the name, Succeeded causes the Check mark to be shown, Failed causes the red X to be shown and enables the error tooltip
        /// and OnDrive indicates that it's already on the WBFS drive so it shouldn't have an icon showing.
        /// </summary>
        public enum CopiedStates { NotYetCopied, Succeeded, Failed, OnDrive };
        /// <summary>
        /// The possible regions for an entry. Used to display the value and to interpret values returned from libwbfs and wbfsInerm
        /// </summary>
        public enum RegionCodes { NTSC, NTSCJ, PAL, KOR, NOREGION };
        #endregion
        #region Constructors
        public WbfsEntry(string entryName, String entryID, float entrySize, CopiedStates copiedState, int index, String coverFilename, RegionCodes regionCode)
        {
            _entryName = entryName;
            _entryID = entryID;
            _entrySize = entrySize;
            _filePath = INVALIDFILE;
            _copiedState = copiedState;
            _index = index;
            _coverImageLocation = coverFilename;
            _regionCode = regionCode;
        }
        public WbfsEntry(string entryName, String entryID, float entrySize, string filePath, float isoSize, CopiedStates copiedState, int index, String coverFilename, RegionCodes regionCode)
        {
            _entryName = entryName;
            _entryID = entryID;
            _entrySize = entrySize;
            _filePath = filePath;
            _copiedState = copiedState;
            _isoSize = isoSize;
            _index = index;
            _coverImageLocation = coverFilename;
            _regionCode = regionCode;
        }
        #endregion
        #region Fields
        //Private backing fields for the proprties mostly.
        private string _entryName;
        private float _entrySize;
        private string _entryID;
        private string _filePath;
        private float _isoSize;
        private RegionCodes _regionCode;
        private CopiedStates _copiedState;
        /// <summary>
        /// The index of the disc on the WBFS drive. Only applicable to discs on the WBFS drive.
        /// </summary>
        private int _index;
        /// <summary>
        /// The index of the disc on the WBFS drive. Only applicable to discs on the WBFS drive.
        /// </summary>
        private string _errorMessageToolTip;
        private string _coverImageLocation;
        #endregion
        #region Properties
        /// <summary>
        /// The game name of this entry
        /// </summary>
        public String EntryName
        {
            get
            {
                return _entryName;
            }
            set
            {
                _entryName = value;
                NotifyPropertyChanged("EntryName");     //If the value is changed, tell the UI to update itself.
            }
        }
        /// <summary>
        /// The size of the entry
        /// </summary>
        public float EntrySize
        {
            get
            {
                return _entrySize;
            }
        }
        /// <summary>
        /// The disc ID for the entry
        /// </summary>
        public String EntryID
        {
            get
            {
                return _entryID;
            }
        }
        /// <summary>
        /// The path to the iso file for this entry. Only applicable to games not already on the WBFS drive.
        /// For games already on the WBFS drive, the value for this should be INVALIDFILE.
        /// </summary>
        public String FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                _filePath = value;
            }
        }
        /// <summary>
        /// The string representation of the size of this entry on WBFS with two decimal points, in GBs.
        /// </summary>
        public String EntrySizeString
        {
            get
            {
                return _entrySize.ToString("N2") + "GB";
            }
        }

        /// <summary>
        /// The size of this entry's source ISO file.
        /// </summary>
        public float IsoSize
        {
            get
            {
                return _isoSize;
            }
        }
        /// <summary>
        /// The string representation of the size of this entry's source ISO file with two decimal points, in GBs.
        /// </summary>
        public String IsoSizeString
        {
            get
            {
                return _isoSize.ToString("N2") + "GB";
            }
        }
        /// <summary>
        /// The region code for this entry. Is one of the possible values from the enumerator.
        /// </summary>
        public RegionCodes RegionCode
        {
            get
            {
                return _regionCode;
            }
        }
        /// <summary>
        /// The string represenation of the region code.
        /// </summary>
        public String RegionCodeString
        {
            get
            {
                switch (_regionCode)
                {
                    case RegionCodes.NTSC:
                        return "NTSC";
                    case RegionCodes.NTSCJ:
                        return "NTSC-J";
                    case RegionCodes.PAL:
                        return "PAL";
                    case RegionCodes.KOR:
                        return "KOR";
                    case RegionCodes.NOREGION:
                        return "RF";
                    default:
                        return "None";
                }
            }
        }
        /// <summary>
        /// One of the values from the CopiedStates enum indicating this entry's status.
        /// </summary>
        public CopiedStates CopiedState
        {
            get
            {
                return _copiedState;
            }
            set
            {
                _copiedState = value;
                NotifyPropertyChanged("CopiedStateImage");      //If the value is changed (i.e. the status has changed) tell the UI to update itself.
            }
        }
        /// <summary>
        /// The icon image to show for the entry's current state.
        /// </summary>
        public ImageSource CopiedStateImage
        {
            get
            {
                switch (_copiedState)
                {
                    case CopiedStates.NotYetCopied:     //if it hasnt yet been copied, dont show any icon.
                        return null;
                    case CopiedStates.Succeeded:        //if it has copied, show the green checkmark icon
                        return new BitmapImage(new Uri("pack://application:,,,/Content/check.png"));
                    case CopiedStates.Failed:           //if it failed for some reason, show the red X error icon
                        return new BitmapImage(new Uri("pack://application:,,,/Content/error.png"));
                    case CopiedStates.OnDrive:          //if its on the drive or the status is unknown dont show any icon
                        return null;
                    default:
                        return null;
                }
            }
        }
        /// <summary>
        /// The entry's index (position) on the WBFS drive. (only valid for entries that are already on the WBFS drive).
        /// </summary>
        public int Index
        {
            get
            {
                return _index;
            }
        }
        /// <summary>
        /// The error message to show on this entry's red error icon tooltip.
        /// </summary>
        public String ErrorMessageToolTip
        {
            get
            {
                if (_copiedState == CopiedStates.Failed)        //only show the tooltip if the status was failed.
                    return _errorMessageToolTip;
                return null;                                    //dont show an error message tooltip if the status isn't failed.
            }
            set
            {
                _errorMessageToolTip = value;
                NotifyPropertyChanged("ErrorMessageToolTip");   
            }
        }
        public String CoverImageLocation
        {
            get
            {
                return _coverImageLocation;
            }
            set
            {
                _coverImageLocation = value;
                NotifyPropertyChanged("ToolTipImage");          //If the value is changed, tell the UI to update itself. (i.e. an error message has been associated with the entry)
            }
        }
        /// <summary>
        /// The cover image to use for the big mouseover tooltip.
        /// </summary>
        public ImageSource ToolTipImage
        {
            get
            {
                if (_coverImageLocation.Equals(NOCOVER) || !Properties.Settings.Default.ShowCovers)     //if no cover exists for this item or covers are disabled, don't show any cover image
                    return null;
                if (_coverImageLocation.Equals(COVERDOWNLOADING))                                       //if the cover image is being downloaded asynchronously show the download in progress placeholder cover
                {
                    if (Properties.Settings.Default.UseWebCovers)                                       //Only if use web covers is still enabled.
                    {
                        return new BitmapImage(new Uri("pack://application:,,,/Content/DownloadingImage.png"));
                    }
                    else
                        return null;                                                                    //if it's been disabled, don't show anything at all
                }
                return new BitmapImage(new Uri(_coverImageLocation, UriKind.Absolute));                 //if it's reached this point, it does have a valid cover image, so show it.
            }
        }
        #endregion
        #region INotifyPropertyChanged Members
        /// <summary>
        /// Tells the UI to check the named property and update the UI accordingly.
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
        #region Methods
        /// <summary>
        /// Checks if the FilePath property is a valid one (i.e. it's not set to INVALIDFILE)
        /// </summary>
        /// <returns></returns>
        public bool IsFilePathValid()
        {
            if (_filePath.Equals(INVALIDFILE))
                return false;
            return true;
        }
        #endregion
    }
}
