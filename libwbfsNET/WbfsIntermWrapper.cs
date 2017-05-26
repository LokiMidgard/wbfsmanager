using System.Runtime.InteropServices;
using System.Text;

namespace libwbfsNET
{
    public class WbfsIntermWrapper
    {
        public delegate void ProgressCallback(int status, int total);
        public delegate void FatalErrorCallback(StringBuilder message);

	    public enum PartitionSelector{
            UPDATE_PARTITION_TYPE=0,
            GAME_PARTITION_TYPE,
            OTHER_PARTITION_TYPE,
            // value in between selects partition types of that value
            ALL_PARTITIONS=-4,
            REMOVE_UPDATE_PARTITION=-3, // keeps game + channel installers
            ONLY_GAME_PARTITION=-2
	    };

        public enum RegionCode{ NTSC, NTSCJ, PAL, KOR, NOREGION };

        /// <summary>
        /// Must be called first. Partition letter must be without ":"
        /// Best practice to subscribe to error event handler prior to calling this or any other methods.
        /// </summary>
        /// <param name="partitionLetter"></param>
        /// <returns></returns>
        [DllImport("libwbfsNETwrapper.dll")]
        public static extern bool OpenDrive(string partitionLetter);

        /// <summary>
        /// Closes the WBFS drive. Do so after opening the drive, prior to formatting.
        /// </summary>
        [DllImport("libwbfsNETwrapper.dll")]
        public static extern void CloseDrive();

        /// <summary>
        /// Formats the drive with the give drive letter. Partition letter must be without ":"
        /// Best practice to subscribe to error event handler prior to calling this or any other methods.
        /// </summary>
        /// <param name="partitionLetter"></param>
        /// <returns></returns>
        [DllImport("libwbfsNETwrapper.dll")]
        public static extern bool FormatDrive(string partitionLetter);

        /// <summary>
        /// Returns the number of discs on the loaded WBFS drive.
        /// Return values:
	    /// -1 : Partition wasn't loaded previously
        /// </summary>
        /// <returns></returns>
        [DllImport("libwbfsNETwrapper.dll")]
        public static extern int GetDiscCount();

        /// <summary>
        /// Gets the discID, size in GB and name of the disc at the given index on the WBFS partition.
        /// StringBuilder discName must have its capacity set prior to call. (256)
        /// Return values:
        /// -1 : Partition wasn't loaded previously
        /// -2 : Invalid index
        /// -3 : Failure reading disc info
        /// </summary>
        /// <param name="index"></param>
        /// <param name="discId"></param>
        /// <param name="size"></param>
        /// <param name="discName"></param>
        /// <returns></returns>
        [DllImport("libwbfsNETwrapper.dll")]
        public static extern int GetDiscInfo(int index, StringBuilder discId, ref float size, StringBuilder discName);

        /// <summary>
        /// Gets the discID, size in GB and name of the disc at the given index on the WBFS partition.
        /// StringBuilder discName must have its capacity set prior to call. (256)
        /// Return values:
        /// -1 : Partition wasn't loaded previously
        /// -2 : Invalid index
        /// -3 : Failure reading disc info
        /// </summary>
        /// <param name="index"></param>
        /// <param name="discId"></param>
        /// <param name="size"></param>
        /// <param name="discName"></param>
        /// <param name="regionCode"></param>
        /// <returns></returns>
        [DllImport("libwbfsNETwrapper.dll")]
        public static extern int GetDiscInfoEx(int index, StringBuilder discId, ref float size, StringBuilder discName, ref RegionCode regionCode);
        
        /// <summary>
        /// Returns the number of used blocks.
        /// Return values:
	    /// -1 : Partition wasn't loaded previously
        /// </summary>
        /// <returns></returns>
        [DllImport("libwbfsNETwrapper.dll")]
        public static extern int GetUsedBlocksCount();

        /// <summary>
        /// Returns the estimated size (in GB) of an ISO if put on a WBFS drive. Also returns disc name and disc id.
        /// Return values:
	    /// -1 : Partition wasn't loaded previously
    	/// -2 : Error occured while attempting to read file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="discId"></param>
        /// <param name="size"></param>
        /// <param name="discName"></param>
        /// <param name="partitionSelection"></param>
        /// <returns></returns>
        [DllImport("libwbfsNETwrapper.dll")]
        public static extern int GetDiscImageInfo(StringBuilder filename, StringBuilder discId, ref float size, StringBuilder discName, [MarshalAs(UnmanagedType.I4)] PartitionSelector partitionSelection);

        /// <summary>
        /// Returns the estimated size (in GB) of an ISO if put on a WBFS drive. Also returns disc name and disc id.
        /// Return values:
        /// -1 : Partition wasn't loaded previously
        /// -2 : Error occured while attempting to read file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="discId"></param>
        /// <param name="size"></param>
        /// <param name="discName"></param>
        /// <param name="regionCode"></param>
        /// <param name="partitionSelection"></param>
        /// <returns></returns>
        [DllImport("libwbfsNETwrapper.dll")]
        public static extern int GetDiscImageInfoEx(StringBuilder filename, StringBuilder discId, ref float size, StringBuilder discName, ref RegionCode regionCode, [MarshalAs(UnmanagedType.I4)] PartitionSelector partitionSelection);

        /// <summary>
        /// Adds an ISO image of a game to the WBFS drive, with the selected paritions. Pass false for copy 1 to 1. libwbfs code seems to have issues if its not set to false.
        /// Return values:
        /// -1 : Partition wasn't loaded previously
        /// -2 : Error occured while attempting to read file
        /// -3 : Disc already exists on WBFS drive
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="progressCallback"></param>
        /// <param name="wiiPartitionToAdd"></param>
        /// <param name="copy1to1"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        [DllImport("libwbfsNETwrapper.dll")]
        public static extern int AddDiscToDrive(StringBuilder filename, ProgressCallback progressCallback, [MarshalAs(UnmanagedType.I4)] PartitionSelector/*int*/ wiiPartitionToAdd, bool copy1to1, StringBuilder newName);

        /// <summary>
        /// Removes a game disc with the given disc ID from the WBFS drive
        /// Return values:
        /// -1 : Partition wasn't loaded previously
        /// -2 : File could not be found on WBFS drive
        /// </summary>
        /// <param name="discId"></param>
        /// <returns></returns>
        [DllImport("libwbfsNETwrapper.dll")]
        public static extern int RemoveDiscFromDrive(StringBuilder discId);

        /// <summary>
        /// Extracts a game disc with the given ID to an ISO image on at the targetName location.
        /// Return values:
        /// -1 : Partition wasn't loaded previously
        /// -2 : File could not be found on WBFS drive
        /// -3 : Unable to open file on disk for writing
        /// </summary>
        /// <param name="discId"></param>
        /// <param name="progressCallback"></param>
        /// <param name="targetName"></param>
        /// <returns></returns>
        [DllImport("libwbfsNETwrapper.dll")]
        public static extern int ExtractDiscFromDrive(StringBuilder discId, ProgressCallback progressCallback, StringBuilder targetName);

        /// <summary>
        /// Rename a game disc with the given discId on the WBFS drive to newName
        /// Return values:
        /// -1 : Partition wasn't loaded previously
        /// -2 : Error occured while renaming
        /// </summary>
        /// <param name="discId"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        [DllImport("libwbfsNETwrapper.dll")]
        public static extern int RenameDiscOnDrive(StringBuilder discId, StringBuilder newName);

        /// <summary>
        /// Subscribe to error events. Includes fatal, error and non-fatal events. Fatal events are treated like normal errors.
        /// </summary>
        /// <param name="errorEventHandler"></param>
        [DllImport("libwbfsNETwrapper.dll")]
        public static extern void SubscribeErrorEvent(FatalErrorCallback errorEventHandler);

        /// <summary>
        /// Returns the statistics of the WBFS drive including the number of used blocks, total, used and free spaces in GBs.
        /// Return values:
        /// -1 : Partition wasn't loaded previously
        /// </summary>
        /// <param name="blocks"></param>
        /// <param name="total"></param>
        /// <param name="used"></param>
        /// <param name="free"></param>
        /// <returns></returns>
        [DllImport("libwbfsNETwrapper.dll")]
        public static extern int GetDriveStats(ref uint blocks, ref float total, ref float used, ref float free);

        /// <summary>
        /// Does direct drive-to-drive copying, if the drive sector sizes are equal, if not returns -101.
        /// Return values:
        /// -1: Source partition wasn't loaded previously
        /// -2: Could not open target partition
        /// -3: Disc is already on the the target partition
        /// -4: Could not open disc on source partition (possibly because it wasn't found)
        /// -101: Drive sector sizes are different
        /// -102: Memory allocation error
        /// -103: No space left on target drive
        /// </summary>
        /// <param name="targetPartitionLetter">Partition letter, without ":"</param>
        /// <param name="progressCallback">A callback function which will handle progress updates</param>
        /// <param name="discId">The disc ID of the game to copy</param>
        /// <returns></returns>
        [DllImport("libwbfsNETwrapper.dll")]
        public static extern int DriveToDriveSingleCopy(StringBuilder targetPartitionLetter, ProgressCallback progressCallback, StringBuilder discId);

        /// <summary>
        /// Check if direct drive-to-drive copying is possible.
        /// Return values:
        /// 0: Success
        /// -1: Source partition wasn't loaded previously
        /// -2: Could not open target partition
        /// -3: Drive sector sizes are different
        /// </summary>
        /// <param name="targetPartitionLetter"></param>
        /// <returns></returns>
        [DllImport("libwbfsNETwrapper.dll")]
        public static extern int CanDoDirectDriveToDrive(StringBuilder targetPartitionLetter);
    }
}
