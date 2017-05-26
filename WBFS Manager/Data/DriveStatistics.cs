using System;
using System.ComponentModel;

namespace WBFSManager.Data
{
    public class DriveStatistics : INotifyPropertyChanged
    {
        public float TotalSpace { get; private set; }
        public String TotalSpaceString
        {
            get
            {
                return TotalSpace.ToString("N2") + " GB";       //Creates a string with 2 decimal spaces
            }
        }
        public float UsedSpace { get; private set; }
        public String UsedSpaceString
        {
            get
            {
                return UsedSpace.ToString("N2") + " GB";
            }
        }
        public float FreeSpace { get; private set; }
        public String FreeSpaceString
        {
            get
            {
                return FreeSpace.ToString("N2") + " GB";
            }
        }
        public uint UsedBlocks { get; private set; }
        public String UsedBlocksString
        {
            get
            {
                return UsedBlocks.ToString();
            }
        }

        public DriveStatistics()
        {
            TotalSpace = -1;
            UsedSpace = -1;
            FreeSpace = -1;
            UsedBlocks = 0;
        }
        public DriveStatistics(uint usedBlocks, float totalSpace, float usedSpace, float freeSpace)
        {
            UsedBlocks = usedBlocks;
            TotalSpace = totalSpace;
            UsedSpace = usedSpace;
            FreeSpace = freeSpace;
            NotifyPropertyChanged("UsedBlocksString");
            NotifyPropertyChanged("TotalSpaceString");
            NotifyPropertyChanged("UsedSpaceString");
            NotifyPropertyChanged("FreeSpaceString");
        }

        public void SetNewStats(uint usedBlocks, float totalSpace, float usedSpace, float freeSpace)
        {
            UsedBlocks = usedBlocks;
            TotalSpace = totalSpace;
            UsedSpace = usedSpace;
            FreeSpace = freeSpace;
            NotifyPropertyChanged("UsedBlocksString");
            NotifyPropertyChanged("TotalSpaceString");
            NotifyPropertyChanged("UsedSpaceString");
            NotifyPropertyChanged("FreeSpaceString");
        }

        #region INotifyPropertyChanged Members
        /// <summary>
        /// Tells the UI to check the named property for changes and update the UI accordingly.
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
    }
}
