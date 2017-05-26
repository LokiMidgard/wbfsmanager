using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Linq;
using libwbfsNET;
using UnRarNET;
using WBFSManager.Data;
using IOPath = System.IO.Path;

namespace WBFSManager
{
    public class WbfsDrive : INotifyPropertyChanged
    {
        #region Fields
        const string HBCLOADERCODER = "WiiCrazy";
        #endregion
        #region Properties
        /// <summary>
        /// List of WbfsEntry items representing the game entries already on the WBFS drive
        /// </summary>
        public ObservableCollection<WbfsEntry> EntriesOnDrive { get; private set; }
        /// <summary>
        /// List of WbfsEntry items representing the games the user intends to add to the WBFS drive 
        /// </summary>
        public ObservableCollection<WbfsEntry> EntriesToAdd { get; private set; }
        /// <summary>
        /// The drive letter corresponding to the drive that's currently loaded as a WBFS drive
        /// </summary>
        public String LoadedDriveLetter { get; set; }
        /// <summary>
        /// A flag indicating the program is busy doing IO using this drive.
        /// </summary>
        public bool Busy { get; private set; }
        /// <summary>
        /// A delegate reference that should be initialized to a function which handles fatal errors raised by libwbfs
        /// </summary>
        public WbfsIntermWrapper.FatalErrorCallback FatalErrorCallback { get; set; }
        /// <summary>
        /// An object of DriveStats representing the different statistics of the drive, total size, free space, etc.
        /// </summary>
        public DriveStatistics DriveStats { get; private set; }
        /// <summary>
        /// The temporary folder to be used for RAR extraction and drive-to-drive copying.
        /// </summary>
        public String TempDirectory { get; set; }
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor, initializes the lists, sets the LoadedDriveLetter to empty, and nulls FatalErrorCallback, DriveStats and TempDirectory
        /// </summary>
        public WbfsDrive()
        {
            EntriesToAdd = new ObservableCollection<WbfsEntry>();
            EntriesOnDrive = new ObservableCollection<WbfsEntry>();
            LoadedDriveLetter = String.Empty;
            FatalErrorCallback = null;
            DriveStats = null;
            TempDirectory = null;
        }
        #endregion
        #region Public Methods
        /// <summary>
        /// Tries to load the drive, if successful clears the list of entries on the drive,
        /// gets the count of discs on the drive, and reads the disc info into WbfsEntry items which are inserted in the EntriesOnDrive list,
        /// Also checks for covers locally and on web and adds it to the WbfsEntry's cover location. 
        /// Uses WbfsIntermWrapper.GetDiscCount and WbfsIntermWrapper.GetDiscInfoEx, WbfsIntermWrapper.OpenDrive(drive) and WbfsIntermWrapper.CloseDrive()
        /// </summary>
        /// <param name="drive">The drive letter corresponding to the drive to be loaded.</param>
        /// <returns>True if the operation was successful, false otherwise.</returns>
        public bool LoadDrive(String drive)
        {
            Busy = true;                                            //Set the drive status to busy
            try
            {
                if (!WbfsIntermWrapper.OpenDrive(drive))            //try to open the drive, if it fails return, otherwise continue.
                {
                    Busy = false;                                   //Set the drive status back to idle
                    return false;                                   //return, since opening the drive was unsuccessful
                }
            }
            catch (AccessViolationException)                        //If an Access Violation occurs, can't open the drive so return
            {
                Busy = false;
                return false;
            }
            LoadedDriveLetter = drive;                              //At this point we're sure the drive was loaded successfully so set the LoadedDrive property
            EntriesOnDrive.Clear();                                 //Clear the list of entries on the drive, in case there are any left over from a previous load operation.
            int discCount = WbfsIntermWrapper.GetDiscCount();       //Get the number of discs on the drive
            for (int i = 0; i < discCount; i++)                     //Iterate over each of the discs on the drive
            {
                StringBuilder discId = new StringBuilder();         //Prepare the pass by ref pointers to be marshalled by P/Invoke
                StringBuilder discName = new StringBuilder(256);
                float size = 0;
                WbfsIntermWrapper.RegionCode regionCode = WbfsIntermWrapper.RegionCode.NOREGION;    //Initially set the region to NOREGION, to make sure the region is loaded correctly.
                int result = WbfsIntermWrapper.GetDiscInfoEx(i, discId, ref size, discName, ref regionCode);  //Make the call to libwbfs wrapper asking for the discinfo of the ith game on the drive
                if (result != 0)                                                                            //if the result is negative it failed to load the game, so skip and move on
                {
#if DEBUG
                    throw new Exception("Error: LoadDrive(),GetDiscInfo() failed for index " + i + " with return value " + result);
#endif
                    continue;
                }
                //Successfully loaded game disc info, so create a WbfsEntry object and add it to EntriesOnDrive list (values are passed back out since the parameters were sent by reference
                WbfsEntry entry = new WbfsEntry(discName.ToString(), discId.ToString(), size, WbfsEntry.CopiedStates.OnDrive, i, WbfsEntry.NOCOVER, (WbfsEntry.RegionCodes)regionCode);
                EntriesOnDrive.Add(entry);
                entry.CoverImageLocation = CheckForCover(entry);        //Check to see if theres a local or online copy of the cover image, and set the Cover Image location for this entry to the result of the call.
            }
            WbfsIntermWrapper.CloseDrive();             //Everything is done, so close the drive again, and set the busy state to false (idle).
            Busy = false;
            return true;
        }
        /// <summary>
        /// LoadDriveInfo opens the drive indicated by LoadedDriveLetter, reads the stats for the drive 
        /// and sets the DriveStats object for the WbfsDrive instance.
        /// Calls WbfsIntermWrapper.OpenDrive(LoadedDriveLetter), WbfsIntermWrapper.GetDriveStats,WbfsIntermWrapper.CloseDrive
        /// </summary>
        /// <returns>True if the operation was successful, false otherwise.</returns>
        public bool LoadDriveInfo()
        {
            Busy = true;                                                    //Set the drive status to busy.
            bool result = WbfsIntermWrapper.OpenDrive(LoadedDriveLetter);   //Open the drive with drive letter equal to LoadedDriveLetter. (set by LoadDrive method)
            if (!result)                                                    //If opening fails, set status to idle and return
            {
                Busy = false;
                return false;
            }
            uint usedBlocks = 0;                                            //Prepare the parameters to be marshalled by reference.
            float totalSpace = -1;
            float usedSpace = -1;
            float freeSpace = -1;
            //Call the libwbfs wrapper to have it read the drive stats and fill in the pass-by-ref parameters.
            if (WbfsIntermWrapper.GetDriveStats(ref usedBlocks, ref totalSpace, ref usedSpace, ref freeSpace) != 0)
            {                                                       //If reading the drive stats fails, set status to idle and return.
                Busy = false;
                return false;
            }
            if (DriveStats == null)                                 //If the DriveStats object hasn't yet been intialized, initialize it using the parameterized constructor
                DriveStats = new DriveStatistics(usedBlocks, totalSpace, usedSpace, freeSpace);
            else
            {                                                       //Otherwise update the objects values to the new values
                DriveStats.SetNewStats(usedBlocks, totalSpace, usedSpace, freeSpace);
            }
            WbfsIntermWrapper.CloseDrive();                         //Close the drive, set the status back to idle and return successfully.
            Busy = false;
            return true;
        }

        /// <summary>
        /// Reloads the drive and reloads the drive statistics.
        /// Calls LoadDrive on the LoadedDriveLetter then calls LoadDriveInfo
        /// </summary>
        /// <returns>
        /// 0: Success
        /// -1: Error loading drive
        /// -2: Error reading drive stats
        /// </returns>
        public int ReloadDrive()
        {
            //WbfsIntermWrapper.CloseDrive();
            if (!LoadDrive(LoadedDriveLetter))
            {
                return -1;
            }
            if (!LoadDriveInfo())
                return -2;
            NotifyPropertyChanged("DriveStats");                //Inform any bindings that the DriveStats property has been changed. (updates any bound UI automatically)
            return 0;
        }

        /// <summary>
        /// Reads game info from an ISO file to be added to the drive, passes out the information using the out parameters.
        /// Passes out the disc ID, estimated size on this drive, disc name and the region code.
        /// Calls WbfsIntermWrapper.OpenDrive(LoadedDriveLetter), WbfsIntermWrapper.GetDiscImageInfoEx, WbfsIntermWrapper.CloseDrive
        /// </summary>
        /// <param name="filename">The full path to the ISO to be read.</param>
        /// <param name="discId">The disc ID will be passed out.</param>
        /// <param name="estimatedSize">The estimated size will be passed out.</param>
        /// <param name="discName">The disc name will be passed out.</param>
        /// <param name="regionCode">The region code will be passed out.</param>
        /// <param name="partitionSelection">The partitions to get the disc image information on.</param>
        /// <returns>
        /// 0: Success
        /// -1: Error Reading (Can't access drive)
        /// -2: Error Estimating
        /// -3: Error Estimating (Loading drive)
        /// -4: Error Estimating (Reading Image)
        /// </returns>
        public int GetDiscImageInfo(String filename, out String discId, out float estimatedSize, out String discName, out WbfsIntermWrapper.RegionCode regionCode, WbfsIntermWrapper.PartitionSelector partitionSelection)
        {
            Busy = true;                                                                //Set the drive status to busy
            bool resultLoad = WbfsIntermWrapper.OpenDrive(LoadedDriveLetter);           //Attempt to open the drive specified by LoadedDriveLetter
            if (!resultLoad)
            {                                                                           //If the load is unsuccessful, pass out invalid values, set the status to idle and return with -1 error code
                discId = string.Empty;
                estimatedSize = -1;
                discName = string.Empty;
                regionCode = WbfsIntermWrapper.RegionCode.NOREGION;
                Busy = false;
                return -1;
            }
            StringBuilder discIdSB = new StringBuilder();                   //If the drive opened successfully, prepare the pass-by-reference parameters to be marshalled by P/Invoke
            StringBuilder discNameSB = new StringBuilder(256);
            float estSize = -1;
            WbfsIntermWrapper.RegionCode rCode = WbfsIntermWrapper.RegionCode.NOREGION;
            int result = WbfsIntermWrapper.GetDiscImageInfoEx(new StringBuilder(filename), discIdSB, ref estSize, discNameSB, ref rCode, partitionSelection);   //Call libwbfs wrapper and get the return code
            WbfsIntermWrapper.CloseDrive();                                                 //Make sure to close the drive immediately to avoid any issues in case the program crashes afterwards.
            if (result < 0)                         //If the result code was negative, the process failed, pass out invalid values, set the status to idle
            {
                discId = string.Empty;
                estimatedSize = -1;
                discName = string.Empty;
                regionCode = WbfsIntermWrapper.RegionCode.NOREGION;
                Busy = false;
                return -2 + result;                             //and return with -2, -3 or -4  depending on what the result code of the operation was (see return values above)
            }
            discId = discIdSB.ToString();                       //If the operation was successful, set the output values correctly, set the drive status to idle and return success (0).
            estimatedSize = estSize;
            discName = discNameSB.ToString();
            regionCode = rCode;
            Busy = false;
            return 0;
        }
        /// <summary>
        /// Parse the array of filename strings, read the data from isos or extract rars and read the iso data, then add the entries to the list of EntriesToAdd.
        /// Calls ExtractRar, this.GetDiscImageInfo
        /// Note: Max recursion depth is two, since Directory.GetFiles gets full paths to all files in the top directory and its subdirectories. Rar extraction might call it recursively the second and last time.
        /// </summary>
        /// <param name="filenames">A array of filename (full path) strings to parse</param>
        /// <param name="worker">The background worker making the call. Used to report progress. Must not be null.</param>
        /// <param name="recursiveCall">Used internally to indicate a recursive call. Must be called with false as inital value, externally.</param>
        /// <param name="enableRarExtraction">Flag indicating whether to extract RAR files and use any ISOs in the RAR files.</param>
        /// <param name="parititonSelection">The paritions that have been selected by the user to use.</param>
        /// <returns>A list of 0: List<!--<WbfsEntry>--> items that were added to the drive. 1: A list of List<!--<ErrorDetails>--> with any error messages that occured.</returns>
        public ArrayList ParseIsoFileList(String[] filenames, BackgroundWorker worker, bool recursiveCall, bool enableRarExtraction, WbfsIntermWrapper.PartitionSelector parititonSelection)
        {
            Busy = true;                                                //Set the drive status to busy.
            List<WbfsEntry> entriesAdded = new List<WbfsEntry>();       //Create a list of the entries that have been added to the drive (since adding to the EntriesToAdd list is not possible (belongs to a separate thread)).
            List<ErrorDetails> errorList = new List<ErrorDetails>();    //Create a list of any errors that occur.
            ArrayList resultList = new ArrayList();                     //An array list to store both entriesAdded and errorList when returning from the function.
            int count =0;                                               //The number of items processed so far.
            foreach (String fileName in filenames)                      //iterate over each of the filename strings
            {
                if (worker.CancellationPending)                         //if the user has requested cancellation, stop as far as we've gotten
                    break;
                if (!recursiveCall)                                     //if this call is not a recursive one, update the progress
                {
                    ArrayList argList = new ArrayList();
                    argList.Add("Overall");                             //Indicate that its an overall progress update
                    argList.Add(filenames.Length);                      //The total number of items to be processed.
                    worker.ReportProgress(count, argList);              //pass the current count and argument list to the progress update callback.
                }
                if (!IOPath.GetExtension(fileName).ToLower().Equals(".iso"))        //If the filename doesnt contain .iso, then it's either a RAR, a directory or irrelavant
                {
                    if (Directory.Exists(fileName))                                 //Check if its a directory
                    {
                        string[] files = Directory.GetFiles(fileName, "*.iso", SearchOption.AllDirectories);        //If so, get all the files in the directory and its subdirectories with .iso extensions
                        string[] rarFiles = Directory.GetFiles(fileName, "*.rar", SearchOption.AllDirectories);     //Also get all the files in the directory and its subdirectories with .rar extensions
                        string[] aggregate = new string[files.LongLength + rarFiles.LongLength];                    //Create a new aggreagated array and copy the values over, to be used for the recursive call to ParseIsoFileList
                        files.CopyTo(aggregate, 0);
                        rarFiles.CopyTo(aggregate, files.LongLength);
                        if (aggregate.Length == 0)                                                                  //If there are no files with .iso or .rar extensions in the folder, then continue on to the next filename in the list of files
                        {
                            count++;                                                                                //make sure to increment count, since one of the items out of the total number to be done has now been processed.
                            continue;
                        }
                        try
                        {
                            ArrayList parseResultList = ParseIsoFileList(aggregate, worker, true, enableRarExtraction, parititonSelection);         //Call ParseIsoFileList recursively to handle the files found in the directory (max depth is 2 (in case theres a rar file in the directory))
                            entriesAdded.AddRange((List<WbfsEntry>)parseResultList[0]);                                         //Add the list of entries added by the recursive call to the list of entries added in the main call
                            List<ErrorDetails> parseDirErrors = (List<ErrorDetails>)parseResultList[1];                         //Add the list of errors generated by the recursive call to the list of errors in the main call, as long as its not null.
                            if (parseDirErrors != null)                                                                         
                                errorList.AddRange(parseDirErrors);
                            count++;                                                                                            //increment the count since one more item has finished processing and move on to the next file in the list.
                            continue;
                        }
                        catch
                        {
                            errorList.Add(new ErrorDetails(Properties.Resources.ErrorAddingStr, Properties.Resources.ErrorAddingShortStr));     //if an exception occurs (due to access violations or rar extraction problems), add an error message indicating so
                            count++;                                                                                            //increment the count since one more item has finished processing and move on to the next file in the list.
                            continue;
                        }
                    }
                    else if (enableRarExtraction && IOPath.GetExtension(fileName).ToLower().Equals(".rar"))                     //if rar extraction is enabled and the file has a .rar extension, extract it and add it to the list of entries added.
                    {
                        ExtractionProgressHandler extractRarProgresHandler = new ExtractionProgressHandler(delegate(object sender, ExtractionProgressEventArgs args)
                            {                                                               //Anonymous function that handles progress updates from the extraction process
                                ArrayList argList = new ArrayList();
                                argList.Add("Item");                                        //Provide progress updates for the item to the background worker (the main progress callback)
                                argList.Add(args.PercentComplete);                          //Include the percent complete in double form
                                argList.Add(args.FileName);                                 //and the filename
                                worker.ReportProgress((int)args.PercentComplete, argList);  //pass the percent complete in integer form as well as the other arguments to the main progress callback
                            });
                        MissingVolumeHandler missingRarVolumeHandler = new MissingVolumeHandler(delegate(object sender, MissingVolumeEventArgs args)
                            {                                                               //Anonymous function that handles errors due to missing rar volumes. Adds an error to the error list and halts the operation
                                errorList.Add(new ErrorDetails(Properties.Resources.ErrorRARMissingVolP1Str + args.VolumeName + Properties.Resources.ErrorRarMissingVolP2Str + fileName, Properties.Resources.ErrorRarMissingVolShortStr));
                                args.ContinueOperation = false;
                            });
                        PasswordRequiredHandler passRequiredRarHandler = new PasswordRequiredHandler(delegate(object sender, PasswordRequiredEventArgs args)
                        {                                                               //Anonymous function that handles errors due to password protected rar files. Adds an error to the error list and halts the operation
                            errorList.Add(new ErrorDetails(Properties.Resources.ErrorRarPassReqdStr + fileName, Properties.Resources.ErrorRarPassReqdShortStr));
                            args.ContinueOperation = false;
                        });                                                         //Note: the about two could be handled to have the user enter the password, etc, but since we're running this on a separate thread, it would be quite tedious and messy.
                        try
                        {
                            if (!Utils.CheckTempForSpace(TempDirectory))                //Make sure the temporary directory provided actually has enough free space for the extraction (checks for 4.8 GB... not entirely correct, since Brawl is 6.8 GBs)
                            {
                                count++;                                                //If theres not enough space, add an error message, increment the number of items processed and move on to the next file.
                                errorList.Add(new ErrorDetails(Properties.Resources.ErrorNotEnoughSpaceTempFolderStr, Properties.Resources.ErrorNotEnoughSpaceShortStr));
                                continue;
                            }
                            List<String> extractedFiles = ExtractRar(fileName, extractRarProgresHandler, missingRarVolumeHandler, passRequiredRarHandler); //Call this.ExtractRar to do the extraction. The return value is a list of iso files that were extracted.
                            if (extractedFiles == null)         //Note that ExtractRar reads the RAR file listing first to check if there even is an iso file inside, if not it'll return null.
                            {
                                count++;
                                continue; //No iso files in the rar archive, increment count of processed files and move on.
                            }
                            ArrayList parseResultList = ParseIsoFileList(extractedFiles.ToArray(), worker, true, enableRarExtraction, parititonSelection);      //Now that the RAR file has been extracted, try to parse the list of files that were extracted
                            entriesAdded.AddRange((List<WbfsEntry>)parseResultList[0]);                                                     //Add any entries processed to the list of entries added
                            List<ErrorDetails> errors = (List<ErrorDetails>)parseResultList[1];                                             //Add any errors that occured to the master list of errors
                            if (errors != null)
                                errorList.AddRange(errors);
                            count++;                                                    //Increment the count since it's been processed and move on to the next file in the main list.
                            continue;
                        }
                        catch (Exception e)
                        {                                                               //If an exception occurs, an error likely occured during the extraction process so add an error message to the master list, increment count and move on.
                            errorList.Add(new ErrorDetails(e.Message, Properties.Resources.ErrorExtractingRARShortStr));
                            count++;
                            continue;
                        }
                    }
                    else
                    {
                        count++;                                                        //If it wasnt an iso or a directory or a rar, it was irrelevant so increment the count (since one of the total number of items was processed) and move on.
                        continue;
                    }
                }
                String discId, discName;                                                //Prepare output parameters to be sent to this.GetDiscImageInfo.
                float estimatedSize = -1;
                WbfsIntermWrapper.RegionCode regionCode = WbfsIntermWrapper.RegionCode.NOREGION;
                int success = GetDiscImageInfo(fileName, out discId, out estimatedSize, out discName, out regionCode, parititonSelection);      //Make the call to this.GetDiscImageInfo and store the result code
                if (success != 0)                           //If the return code was not 0, an error occured. If so, add an error message to the error list, incrementer count and move on to the next file.
                {
                    if (success == -1)
                        errorList.Add(new ErrorDetails(Properties.Resources.ErrorReadingISOStr, Properties.Resources.ErrorReadingISOShortStr));
                    else if (success == -2)
                        errorList.Add(new ErrorDetails(Properties.Resources.ErrorEstimatingStr + fileName + ".", Properties.Resources.ErrorEstimatingShortStr));
                    else if (success == -3)
                        errorList.Add(new ErrorDetails(Properties.Resources.ErrorEstimatingStr + fileName + ". " + Properties.Resources.ErrorEstimateLoadingDriveStr, Properties.Resources.ErrorEstimatingShortStr));
                    else if (success == -4)
                        errorList.Add(new ErrorDetails(Properties.Resources.ErrorEstimatingStr + fileName + ". " + Properties.Resources.ErrorEstimateReadingImageStr, Properties.Resources.ErrorEstimatingShortStr));
                    count++;
                    continue;
                }
                FileInfo fi = new FileInfo(fileName);                                   //Create a FileInfo object from the file's full path so as to get file size (ISO size displayed on the UI).
                //Create a WbfsEntry object from the file and the values obtained from this.GetDiscImageInfo. The entry is created initially without a cover (hence NOCOVER), set as not yet copied so that no icon is shown, set to no index since it's not already on the WBFS drive
                WbfsEntry entry = new WbfsEntry(discName, discId, estimatedSize, fileName, (fi.Length * 1.0f) / (1024 * 1024 * 1024), WbfsEntry.CopiedStates.NotYetCopied, -1, WbfsEntry.NOCOVER, (WbfsEntry.RegionCodes)regionCode);
                //EntriesToAdd.Add(entry);              //Cant directly add to the EntriesToAdd since it belongs to a different thread, so add it to the local entriesAdded list, which will be added to EntriesToAdd by the owner thread.
                entriesAdded.Add(entry);
                entry.CoverImageLocation = CheckForCover(entry);        //Check to see if theres a local or online copy of the games cover and set the coverImageLocation. (if there isn't the value will be NOCOVER (which is :NOCOVER:) so that the UI knows and doesnt try to load it). Note that ":" is an invalid filename character so an attempt to actually load it would raise an exception.
                count++;                                                //Increment count of completed items and allow the for loop to move on to the next file in the list.
            }
            if(!recursiveCall)                                          //If this call isn't a recursive call, then were really done so we can set the Busy state back to false (idle).
                Busy = false;
            resultList.Add(entriesAdded);                               //Add the entriesAdded and errorList into our resultList and send it back as the return value.
            resultList.Add(errorList);
            return resultList;
        }

        /// <summary>
        /// Called by bwExtract_DoWork to do handle the extraction of a single or multiple entries from the WBFS drive.
        /// Calls WbfsIntermWrapper.OpenDrive(LoadedDriveLetter), WbfsIntermWrapper.ExtractDiscFromDrive and WbfsIntermWrapper.CloseDrive
        /// </summary>
        /// <param name="entriesToExtract">The list of entries to extract from the WBFS drive.</param>
        /// <param name="targetFilename">The filename to save to (if theres a single entry in the entriesToExtract list), or directory to extract to (if there are multiple entries in the list).</param>
        /// <param name="worker">The background worker making the call. Used to report progress. Must not be null.</param>
        /// <returns>A list of errors that occured, empty if no errors.</returns>
        public List<ErrorDetails> ExtractDiscFromDrive(IList entriesToExtract, String targetFilename, BackgroundWorker worker)
        {
            Busy = true;                                                //Set the drive status to busy
            int count = 0;                                              //Counter for the number of items processed so far.
            List<ErrorDetails> errors = new List<ErrorDetails>();       //List of errors that have occured
            WbfsIntermWrapper.ProgressCallback pc = new WbfsIntermWrapper.ProgressCallback(delegate(int val, int total)
            {                                                           //Anonymous function to handle progress updates from libwbfs
                ArrayList argList = new ArrayList();      //create a list of argumetns to pass to the background worker's ReportProgress callback
                argList.Add("Item");                                    //Indicate that it's a progress update on the current item
                argList.Add(total);                                     //pass the total number of elements to be processed.
                worker.ReportProgress(val, argList);                    //Call the callback, passing the current value (passed by the libwbfs callback) and the argument list
            });
            if (entriesToExtract.Count == 1)                            //if theres a single item (use the targetFilename as an actual filename instead of a folder)
            {
                bool resultLoad = WbfsIntermWrapper.OpenDrive(LoadedDriveLetter);       //Attempt to load the drive indicated by LoadDriveLetter. Save the return code.
                if (!resultLoad)                                                        //if the result was false, an error occured
                {                                                                       //Add an error message to the list, set the drive status to idle and return (since there was only one item)
                    errors.Add(new ErrorDetails(Properties.Resources.ErrorExtractingStr, Properties.Resources.ErrorExtractingShortStr));
                    Busy = false;
                    return errors;
                }
                //Call libwbfs to do the drive extraction process
                int result = WbfsIntermWrapper.ExtractDiscFromDrive(new StringBuilder(((WbfsEntry)entriesToExtract[0]).EntryID), pc, new StringBuilder(targetFilename));
                WbfsIntermWrapper.CloseDrive();         //Make sure to close the drive right away in case anything goes wrong and the program crashes, to avoid any open handles being leftover.

                if (result != 0)                        //If the return code of the ExtractDiscFromDrive wbfsInterm method was not zero, an error occured
                {
                    String extra = String.Empty;        //Prepare an appropriate error message based on what the error code was.
                    if (result == -1)
                        extra = Properties.Resources.ErrorAddLoadingStr;
                    else if (result == -2)
                        extra = Properties.Resources.ErrorExtrNoGameStr;
                    else if (result == -3)
                        extra = Properties.Resources.ErrorExtrNoDriveStr;               //Add the error message to the list of errors
                    errors.Add(new ErrorDetails(Properties.Resources.ErrorExtractingP1Str + ((WbfsEntry)entriesToExtract[0]).EntryName + extra, Properties.Resources.ErrorExtractingP1ShortStr));
                    Busy = false;                                                       //set the drive status to idle and return with the error list (since theres only one item to process)
                    return errors;
                }
            }
            else                                                                        //There was more than one item in the list, so treat the targetFilename as a directory to put all the ISOs in
            {
                ArrayList argListOverall = new ArrayList();
                argListOverall.Add("Overall");
                argListOverall.Add(entriesToExtract.Count);                         //Set the arguments to "Overall" to indicate an overall progress updates and also pass the total number of entries, and the current number completed.
                foreach (WbfsEntry item in entriesToExtract)                            //for every entry in the list, extract it and update the overall progress
                {
                    if (worker.CancellationPending)                                     //if the user has requested cancellation, stop as far as we've gotten.
                        break;
                    worker.ReportProgress(count, argListOverall);                       //prepare the argumetns for the overall progress report and report at the beggining of each iteration (to avoid having to do this before every continue)
                    bool resultLoad = WbfsIntermWrapper.OpenDrive(LoadedDriveLetter);   //Attempt to open the drive indicated by LoadedDriveLetter
                    if (!resultLoad)                                                    //If the result code is false, an error occured stop the whole operation since the drive can't be opened.
                    {
                        errors.Add(new ErrorDetails(Properties.Resources.ErrorExtractingStr, Properties.Resources.ErrorExtractingShortStr));    //add error message to the list, set the drive status to idle and return from the whole opeartion.
                        Busy = false;
                        return errors;
                    }
                    String itemFilename = item.EntryName + ".iso";                      //Create a filename for the iso using the game's name

                    foreach (char invalidChar in Path.GetInvalidFileNameChars())        //Strip out the invalid filename characters from the game's name
                    {
                        itemFilename = itemFilename.Replace(invalidChar, '_');
                    }
                    //Call libwbfs to do the drive extraction process
                    int result = WbfsIntermWrapper.ExtractDiscFromDrive(new StringBuilder(item.EntryID), pc, new StringBuilder(Path.Combine(targetFilename, itemFilename)));
                    WbfsIntermWrapper.CloseDrive();     //Make sure to close the drive right away in case anything goes wrong and the program crashes, to avoid any open handles being leftover.

                    if (result != 0)                    //If the return code of the ExtractDiscFromDrive wbfsInterm method was not zero, an error occured
                    {
                        String extra = String.Empty;    //Prepare an appropriate error message based on what the error code was.
                        if (result == -1)
                            extra = Properties.Resources.ErrorAddLoadingStr;
                        else if (result == -2)
                            extra = Properties.Resources.ErrorExtrNoGameStr;
                        else if (result == -3)
                            extra = Properties.Resources.ErrorExtrNoDriveStr;
                        errors.Add(new ErrorDetails(Properties.Resources.ErrorExtractingP1Str + item.EntryName + extra, Properties.Resources.ErrorExtractingP1ShortStr));
                        //Add the error message to the list of errors, set the item state to failed, and set the error tooltip to the error message. (setting the state causes the appropriate failed icon to be displayed by the UI automagically :p)
                        item.CopiedState = WbfsEntry.CopiedStates.Failed;
                        item.ErrorMessageToolTip = Properties.Resources.ErrorExtractingP1Str + item.EntryName + extra;
                        count++;                //Increment the count since we finished processing one item (even though it failed), then move on to the next one
                        continue;
                    }
                    item.CopiedState = WbfsEntry.CopiedStates.Succeeded;        //If the error code was zero, it succeeded, so set the state to succeeded, increment count and let the for loop move on to the next iteration
                    count++;
                }
            }
            Busy = false;                                                       //Once all the processing is done, set the state back to idle and return the list of any errors that might have occured.
            return errors;
        }

        /// <summary>
        /// Called by bw_AddToDriveDoWork to add the items specified by the WbfsEntry's conatined in the list to the WBFS drive.
        /// Calls WbfsIntermWrapper.OpenDrive(LoadedDriveLetter), WbfsIntermWrapper.AddDiscToDrive, WbfsIntermWrapper.CloseDrive
        /// </summary>
        /// <param name="entries">A list of WbfsEntry's to add to the WBFS drive. These entries must have their FilePath set.</param>
        /// <param name="worker">The background worker object making the call. Used to report progress. Must not be null.</param>
        /// <param name="partitionSelection">The partitions of the Wii disc to add to the drive.</param>
        /// <returns>A list of any errors that were generated in the process. If list count is zero, no errors were generated.</returns>
        public List<ErrorDetails> AddToDrive(IList<WbfsEntry> entries, BackgroundWorker worker, WbfsIntermWrapper.PartitionSelector partitionSelection)
        {
            Busy = true;                                                        //Set drive status to busy
            int count = 0;                                                      //The number of items that have been completed.
            List<ErrorDetails> errors = new List<ErrorDetails>();               //The list of errors that have occured.
            WbfsIntermWrapper.ProgressCallback pc = new WbfsIntermWrapper.ProgressCallback(delegate(int val, int total)
            {                                                                   //Anonymous function to handle progress updates from libwbfs.
                ArrayList argList = new ArrayList();
                argList.Add("Item");                                            //Prepare the arguments for the background worker's progress update callback
                argList.Add(total);                                             //inidcate its an "item" update, include the total number of items, and the current value (as reported by libwbfs)
                worker.ReportProgress(val, argList);                            //invoke the bkg worker's callback
            });
            
            ArrayList argListOverall = new ArrayList {"Overall", entries.Count};       //Prepare the arguments for the overall update, and do an update at the beginning of each iteration (to avoid having to do it before every continue)
            foreach (WbfsEntry entry in entries)                                //iterate over all the entries in the list of entries to be added.
            {
                if (worker.CancellationPending)                                 //if the user has requested cancellation, stop the operation as far as its gotten already.
                    break;
                worker.ReportProgress(count, argListOverall);   //count * 100 / (entries.Count * 1),"Overall");     //No calculation necessary here.
                if (!entry.IsFilePathValid())                                   //Make sure the current entry's filePath has a valid value (i.e. not set to INVALIDFILE).
                {
                    count++;                                                    //if the values invalid, increment the number of items processed and move on to the next iteration.
                    continue;
                }
                //string output;                                                //Removed this error message to avoid interrupting the process and requiring input (allows "unattended installs")
                //if (!LaunchExec("add \"" + entry.FilePath + "\"", true, out output))
                //{
                //    MessageBox.Show("Error occured while adding: " + entry.EntryName + " : " + output, "Error adding game", MessageBoxButton.OK, MessageBoxImage.Error);
                //    entry.SuccessfullyCopied = false;
                //    continue;
                //}

                bool resultLoad = WbfsIntermWrapper.OpenDrive(LoadedDriveLetter);           //Attempt to open the drive indicated by LoadedDriveLetter
                if (!resultLoad)                                                            //If the result is false, an error occured
                {                                                                           //show an error message, set the drive to idle and return with the error list (since errors opening the drive are fatal to the addition process)
                    errors.Add(new ErrorDetails(Properties.Resources.ErrorAddingNoDriveStr, Properties.Resources.ErrorAddingNoDriveShortStr));
                    Busy = false;
                    return errors;
                }
                //Call wbfsInterm (which calls libwbfs) to add the disc to the WBFS drive.
                int result = WbfsIntermWrapper.AddDiscToDrive(new StringBuilder(entry.FilePath), pc, partitionSelection, false, new StringBuilder(entry.EntryName));
                WbfsIntermWrapper.CloseDrive();                                             //Make sure to close the drive right away to avoid any leftover handles in case the program crashes.
                if (result != 0)                                                            //If the result code is not zero, an error has occured.
                {
                    String extra = String.Empty;                                            //Create an error message based on what the error code was.
                    if (result == -1)
                        extra = Properties.Resources.ErrorAddLoadingStr;
                    else if (result == -2)
                        extra = Properties.Resources.ErrorAddReadStr;
                    else if (result == -3)
                        extra = Properties.Resources.ErrorAddExistsStr;

                    entry.CopiedState = WbfsEntry.CopiedStates.Failed;                      //Set the CopiedState to Failed (which shows the failed icon), add the error tooltip , and also add the error to the list of errors generated.
                    entry.ErrorMessageToolTip = Properties.Resources.ErrorWhileAddingStr + " " + entry.EntryName + extra;
                    errors.Add(new ErrorDetails(Properties.Resources.ErrorWhileAddingStr + " " + entry.EntryName + extra, Properties.Resources.ErrorWhileAddingShortStr));
                    count++;
                    continue;                                                               //One more item processed so increment count and move on to the next iteration.
                }
                entry.CopiedState = WbfsEntry.CopiedStates.Succeeded;                       //If it gets here, the process succeded, so set the copied state to successful, increment the count of processed items and let the for loop move to the next iteration.
                count++;

            }
            Busy = false;                                                                   //Set the drive state back to idle and return the list of any errors that were generated in the process.
            return errors;
        }

        /// <summary>
        /// Creates a Homebrew Channel entry for the WBFS entry that's passed.
        /// </summary>
        /// <param name="entry">The WBFS entry to create a HBC entry for.</param>
        /// <param name="outputFolder">The folder to put the created files in.</param>
        /// <param name="hbcDolFullPath">The path to the yal boot.dol. (Can use other .dol Loaders as well)</param>
        /// <param name="hbcPngFullPath">The path to the icon.png used by the HBC.</param>
        /// <param name="dolOriginalTitleID">The placeholder, original title ID of the loader .dol file</param>
        /// <returns>True indicating success or false otherwise.</returns>
        public bool CreateHbcEntry(WbfsEntry entry, String outputFolder, string hbcDolFullPath, string hbcPngFullPath, string dolOriginalTitleID)
        {
            Busy = true;                                                        //Set the drive status to busy (not entirely necessary for this since it's not actually accessing the drive.
            if (!File.Exists(hbcDolFullPath))                                   //If the boot.dol file is missing, set the drive back to idle and return with failure result
            {
                Busy = false;
                return false;
            }
            try
            {
                String entryName = entry.EntryName;
                foreach (char invalidChar in Path.GetInvalidFileNameChars())        //Strip out the invalid filename characters from the game's name
                {
                    entryName = entryName.Replace(invalidChar, '_');
                }
                String fullPath = IOPath.Combine(outputFolder, entryName);      //Use the specified output folder and the game's ID to create a full path for the HBC entry. (For yal boot.dol the folder name must match the game ID to be booted from the WBFS drive.)
                Directory.CreateDirectory(fullPath);                                //Create the directory for this HBC entry.

                //File.Copy(hbcDolFullPath, IOPath.Combine(fullPath, "boot.dol"), true);      //Copy the boot.dol over to the target directory.
                byte[] data = File.ReadAllBytes(hbcDolFullPath);
                bool idReplaced = false;
                for (long i = 0; i < data.LongLength; i++)
                {
                    if (data[i] == dolOriginalTitleID[0] && data[i + 1] == dolOriginalTitleID[1] && data[i + 2] == dolOriginalTitleID[2] && data[i + 3] == dolOriginalTitleID[3] && data[i + 4] == dolOriginalTitleID[4] && data[i + 5] == dolOriginalTitleID[5])
                    {
                        byte[] bytesDiscId = Encoding.ASCII.GetBytes(entry.EntryID);
                        data[i] = bytesDiscId[0];
                        data[i + 1] = bytesDiscId[1];
                        data[i + 2] = bytesDiscId[2];
                        data[i + 3] = bytesDiscId[3];
                        data[i + 4] = bytesDiscId[4];
                        data[i + 5] = bytesDiscId[5];
                        idReplaced = true;
                        break;
                    }
                }
                if (!idReplaced)
                    return false;
                if (File.Exists(IOPath.Combine(fullPath, "boot.dol")))
                    File.Delete(IOPath.Combine(fullPath, "boot.dol"));
                File.WriteAllBytes(IOPath.Combine(fullPath, "boot.dol"), data);

                if (File.Exists(hbcPngFullPath))                                            //If the icon.png file exists copy it over to the target directory. Since this file isn't required by HBC, it's ok if the file doesn't exist, we can still successfully create an HBC entry.
                    File.Copy(hbcPngFullPath, IOPath.Combine(fullPath, "icon.png"), true);
                XDocument meta = new XDocument(                                             //Create the meta.xml document (using LINQ)
                    new XDeclaration("1.0", "UTF-8", "yes"),                                //Generate the xml declaration
                        new XElement("app",
                            new XElement("name", entry.EntryName),                          //Set the name element of the xml doc to the name of the WBFS entry
                            new XElement("coder", HBCLOADERCODER),
                            new XElement("short_description", entry.EntrySizeString + Properties.Resources.MetaXmlShortDescStr),   //Set the short description to the typical one used by wbfs.exe
                            new XElement("long_description", Properties.Resources.MetaXmlLongDescStr + entry.EntryName)));            //Set the long description to credit Kwiirk for yal.
                meta.Save(IOPath.Combine(fullPath, "meta.xml"));                            //Save the generated xml file.
                Busy = false;                                                               //Set the drive status back to idle
                return true;                                                                //return successfully.
            }
            catch (Exception exc)
            {                               //Catch any unexpected errors (probably IO errors) and return unsuccesfully. Show an error message only if in debug mode.
#if DEBUG
                MessageBox.Show("Error occured while creating HBC entry: " + exc.Message + " " + exc.StackTrace);
#endif
                return false;
            }
        }

        /// <summary>
        /// Deletes the specified entry off of the WBFS drive.
        /// Calls WbfsIntermWrapper.OpenDrive(LoadedDriveLetter), WbfsIntermWrapper.RemoveDiscFromDrive, WbfsIntermWrapper.CloseDrive, this.ReloadDrive
        /// </summary>
        /// <param name="entryToDelete">A WbfsEntry object referencing the entry to be deleted.</param>
        /// <returns>A ErrorDetails object if an error occurs, or null if no error occurs.</returns>
        public ErrorDetails DeleteGameFromDisc(WbfsEntry entryToDelete)
        {
            Busy = true;                                                                    //Set the drive status to busy.
            int result = 1;                                                                 //set the initial value of the result to a return code not used by the method.
            bool resultLoad = WbfsIntermWrapper.OpenDrive(LoadedDriveLetter);           //Attempt to open the drive indicated by LoadedDriveLetter
            if (!resultLoad)                                                            //If the result is false, an error occured
            {                                                                           //show an error message, set the drive to idle and return with the error list (since errors opening the drive are fatal to the deletion process)
                Busy = false;
                return new ErrorDetails(Properties.Resources.ErrorDeleteStr, Properties.Resources.ErrorDeleteShortStr);
            }
            StringBuilder discId = new StringBuilder(entryToDelete.EntryID);            //prepare the discID of the game to delete to be marshalled to wbfsInterm (and libwbfs)
            result = WbfsIntermWrapper.RemoveDiscFromDrive(discId);                     //Make the call to wbfsInterm to remove the game with the discID of the entry to be deleted.
            WbfsIntermWrapper.CloseDrive();                                             //Make sure to close the drive to avoid leftover handles in case the program crashes.
            if (result != 0)                                                            //If the result code is anything but zero, an error occured.
            {
                String extra = String.Empty;                                            //Create an error message based on the error code and return an error details object based on the message.
                if (result == -2)
                    extra = Properties.Resources.ErrorDeleteNotFoundStr;
                Busy = false;
                return new ErrorDetails(Properties.Resources.ErrorDeleteStr + extra, Properties.Resources.ErrorDeleteShortStr);
            }
            //Once the entry has been delete, reload the drive and drive info by calling this.ReloadDrive. which will cause the EntriesOnDrive to be updated and will also update the drive stats.
            result = ReloadDrive();
            if (result == -1)                                                           //If the result is -1 the method wasn't able to load the drive succesffully, so display an error message.
            {
                Busy = false;                                                           //Set the drive back to idle and return with an error message.
                return new ErrorDetails(Properties.Resources.ErrorLoadingDriveStr, Properties.Resources.ErrorLoadingDriveShortStr);
            }
            else if (result == -2)                                                      //If the result is -2, there was an error loading the drive stats.
            {
                Busy = false;                                                           //Set the drive back to idle and return with an error message.
                return new ErrorDetails(Properties.Resources.ErrorReadingDriveStatsStr, Properties.Resources.ErrorReadingDriveStatsShortStr);
            }
            Busy = false;                                                               //The operation was successful, so set the drive back to idle and return null, indicating no error
            return null;
        }

        /// <summary>
        /// Formats a drive of any format to WBFS format. Be careful with this! Make sure to prompt the user before executing this method.
        /// Calls WbfsIntermWrapper.CloseDrive, WbfsIntermWrapper.FormatDrive(driveLeteter), this.ReloadDrive
        /// </summary>
        /// <param name="driveLeteter">The drive letter corresponding to the drive to format.</param>
        /// <returns>An ErrorDetails object with an error message if an error occurrs, null if no errors.</returns>
        public ErrorDetails FormatWbfsDrive(String driveLeteter)
        {
            Busy = true;                                                                //Set the drive status to busy
            if (driveLeteter.Trim().Length == 0)                                        //Make sure theres an actual drive letter being passed in.
            {
                Busy = false;                                                           //if not, set the drive status back to idle and return an error message
                return new ErrorDetails(Properties.Resources.ErrorFormatInvalidStr, Properties.Resources.ErrorFormatInvalidShortStr);
            }
            

            if (!Utils.CheckForWindowsXP())                                             //Windows XP behaves differently to closing a drive that might already be closed
            {                                                                           //If it's not Windows XP, close the drive
                try
                {
                    WbfsIntermWrapper.CloseDrive();
                }
                catch (AccessViolationException ave)                                        //This shouldn't really ever happen, but in case, set the drive status to idle and return an error message.
                {
                    
                    
#if DEBUG
                    Busy = false;
                    throw ave;                                                              //if in debug mode throw an exception so we know that this has occured
                    return new ErrorDetails(Properties.Resources.ErrorFormatClosingDriveStr, Properties.Resources.ErrorFormatShortStr);
#endif

                }
            }
            try
            {
                if (!WbfsIntermWrapper.FormatDrive(driveLeteter))       //Try to format the drive with the specified drive letter. If the result is false, an error occured
                {
                    Busy = false;                                       //if an error occured set the drive status back to idle and return with an error message.
                    return new ErrorDetails(Properties.Resources.ErrorFormatStr, Properties.Resources.ErrorFormatShortStr);
                }
                
            }
            catch (AccessViolationException)                            //If an access violation occurs here, the user is trying to format a drive they shouldn't be (a DVD drive, the Windows drive!, etc).
            {
                Busy = false;                                           //If the exception is raised set the status back to idle
                return new ErrorDetails(Properties.Resources.ErrorFormatStr, Properties.Resources.ErrorFormatShortStr);     //return with an error message.
            }
            LoadedDriveLetter = driveLeteter;                           //Set the loaded drive letter to the letter of the drive that was formatted
            WbfsIntermWrapper.CloseDrive();                             //Close the drive before reloading it to avoid any issues.
            int result = ReloadDrive();                                 //Reload the drive and the drive statistics.
            if (result == -1)                                           //If the result is a -1 an error occurred when loading the drive, status back to idle and return an error message.
            {
                Busy = false;
                return new ErrorDetails(Properties.Resources.ErrorLoadingDriveStr, Properties.Resources.ErrorLoadingDriveShortStr);
            }
            else if (result == -2)                                      //if the result is a -2, an error occurred while reading the drive stats, status back to idle, return an error message. 
            {
                Busy = false;
                return new ErrorDetails(Properties.Resources.ErrorReadingDriveStatsStr, Properties.Resources.ErrorReadingDriveStatsShortStr);
            }
            Busy = false;                                               //if it gets here, the operation was succcessful, so set the status back to idle and return null to indicate no errors.
            return null;
        }

        /// <summary>
        /// Rename a WBFS entry thats already on the drive.
        /// </summary>
        /// <param name="entryToRename">The WbfsEntry object referring to the entry to be rename</param>
        /// <param name="newName">The new name for the entry.</param>
        /// <returns>An ErrorDetails object with a message if an error occurs, null if no errors occur.</returns>
        public ErrorDetails RenameDiscOnDrive(WbfsEntry entryToRename, String newName)
        {
            Busy = true;                                                //Set the drive status to idle
            StringBuilder discId = new StringBuilder(entryToRename.EntryID);        //Prepare the discID and new name string to be marshalled
            StringBuilder newNameSb = new StringBuilder(newName);
            bool resultLoad = WbfsIntermWrapper.OpenDrive(LoadedDriveLetter);           //Attempt to open the drive indicated by LoadedDriveLetter
            if (!resultLoad)                                                            //If the result is false, an error occured
            {                                                                           //show an error message, set the drive to idle and return with the error list (since errors opening the drive are fatal to the rename process)
                Busy = false;
                return new ErrorDetails(Properties.Resources.ErrorRenameStr, Properties.Resources.ErrorRenameShortStr);
            }
            int result = WbfsIntermWrapper.RenameDiscOnDrive(discId, newNameSb);        //Call wbfsInterm to rename the disc with the ID matching this entry's ID to the specified name.
            WbfsIntermWrapper.CloseDrive();                                             //Make sure to close the drive to avoid any leftover handles in case the program crashes.
            if (result < 0)                                                             //If the rename operations return code was negative an error occured.
            {
                String extra = String.Empty;                                            //Create an error message depending on the error code, set status to idle and return the error message.
                if (result == -2)
                    extra = Properties.Resources.ErrorRenameCantLoadStr;
                Busy = false;
                return new ErrorDetails(Properties.Resources.ErrorRenameStr + extra, Properties.Resources.ErrorRenameShortStr);
            }
            entryToRename.EntryName = newName;                                          //If it's gotten this far, the operation was successful, so update the entry's name in our EntriesOnDrive list (automagically updating the UI).
            Busy = false;                                                               //Set the status back to idle and return null indicating no errors occured.
            return null;
        }

        /// <summary>
        /// Force an update of the cover image paths for the WbfsEntry objects in both the EntriesToAdd and EntriesOnDrive lists.
        /// </summary>
        public void UpdateCoverImages()
        {
            if (EntriesToAdd.Count > 0)                                 //If the EntriesToAdd list has at least 1 item, iterate over each item
            {
                foreach (WbfsEntry item in EntriesToAdd)
                {
                    item.CoverImageLocation = CheckForCover(item);      //Call this.CheckForCover to look for a cover image and save the path thats returned as the CoverImageLocation for the current entry
                }
            }
            if (EntriesOnDrive.Count > 0)                               //If the EntriesToAdd list has at least 1 item, iterate over each item
            {
                foreach (WbfsEntry item in EntriesOnDrive)
                {
                    item.CoverImageLocation = CheckForCover(item);      //Call this.CheckForCover to look for a cover image and save the path thats returned as the CoverImageLocation for the current entry
                }
            }
        }
        #endregion
        #region Private Methods
        /// <summary>
        /// Checks for a cover image for the given entry, locally initially, if it doesn't find it locally it calls CheckForCoverWeb to check online (depending on whether the user has enabled that option).
        /// </summary>
        /// <param name="entry">The entry to get a cover for.</param>
        /// <returns>The path to the cover file or WbfsEntry.NOCOVER if none is found, or WbfsEntry.COVERDOWNLOADING if the file is being downloaded (async)</returns>
        private string CheckForCover(WbfsEntry entry)
        {
            try
            {
                if (Properties.Settings.Default.CoverDirs != null)              //If the list of cover directories in the settings file is not null, start checking those directories for this entry's cover image.
                {
                    foreach (String directoryStr in Properties.Settings.Default.CoverDirs)      //Iterate over the user specified cover directories.
                    {
                        if (Directory.Exists(directoryStr))                                     //If the cover directory actually exists
                        {
                            string[] result = Directory.GetFiles(directoryStr, entry.EntryID + ".png", SearchOption.AllDirectories);   //Get a list of paths to any PNG files that are named with the entry's ID in that directory and its subdirectories.
                            if (result.Length != 0)                                             //If at least one PNG file was found, return the first one as the path to this entry's cover image.
                            {
                                return result[0];
                            }
                        }
                    }
                }                                                                               //If it gets to this point and it hasn't returned yet, it means it hasnt' found it in any of the user's cover directories
                String fullCoverPath = GetMyDocsCoverFullPath();                                //Retrieve the full path to the default My Documents\WBFS Manager Covers directory
                if (Directory.Exists(fullCoverPath))                                            //If the directory actually exists, look for a PNG file with this entry's ID as the name in the root directory and all its subdirectories.
                {
                    string[] fileList = Directory.GetFiles(fullCoverPath, entry.EntryID + ".png", SearchOption.AllDirectories);
                    if (fileList.Length != 0)                                                   //If at least one such file is found, return the first one found as the path to this entry's cover image.
                        return fileList[0];
                }                                                                               //If it's gotten this far without returning, the cover image isn't in the My Docs folder either
                if (Properties.Settings.Default.UseWebCovers)                                   //If the user's settings indicate that the user has enabled checking for covers on the web
                    return CheckForCoverWeb(entry);                                             //call CheckForCoverWeb to take care of finding the cover image and return whatever string CheckForCoverWeb returns as the path to the entry's cover image.
                else
                    return WbfsEntry.NOCOVER;                                                   //otherwise give up and return WBfsEntry.NOCOVER to indicate a non-existent cover image. (since NOCOVER is ":NOCOVER:" it's impossible to mistake it for a real path (invalid filename chars)
            }
            catch                                                                               //If an exception occurs at any point, give up and return NOCOVER.
            {
                return WbfsEntry.NOCOVER;
            }
        }

        /// <summary>
        /// Checks for the specified entry's cover image on the web, using the web links provided in the settings file.
        /// Web links must end in a query string asking for the 6 digit disc ID.
        /// Downloading is done asynchronously, so this method returns WbfsEntry.COVERDOWNLOADING, when the download is complete, it updates the CoverImageFileLocaiton with the path
        /// to the downloaded cover image (or WbfsEntry.NOCOVER, if it didn't find the cover online either).
        /// </summary>
        /// <param name="entry">The entry to get a cover for from the web.</param>
        /// <returns>Returns WbfsEntry.COVERDOWNLOADING.</returns>
        private string CheckForCoverWeb(WbfsEntry entry)
        {
            WebClient wc = new WebClient();                                     //Create a WebClient object to do the downloading.
            String fullCoverPath = GetMyDocsCoverFullPath();                    //Get the full path to My Documents\WBFS Manager Covers
            try
            {
                if (!Directory.Exists(fullCoverPath))                               //Check if the directory actually exists.
                    Directory.CreateDirectory(fullCoverPath);                       //if not, create it.
            }
            catch
            {
                return WbfsEntry.NOCOVER;                                       //If an exception occurrs, an access violation has likely occurred, so give up and show no cover
            }
            String fullFilename = IOPath.Combine(fullCoverPath, entry.EntryID + ".png");        //Create a full filename for the image to be saved as, using the disc ID.
            wc.DownloadFileCompleted += new AsyncCompletedEventHandler(wc_DownloadFileCompleted);       //Set the callback for when the asynchronous download finishes.
            BackgroundWorker bw = new BackgroundWorker();                                       //A background worker to make the WebClient async call. Using this to keep the UI live and not block here. (is most likely unncessary, but doesn hurt ...)
            bw.DoWork += new DoWorkEventHandler(
                delegate(object sender, DoWorkEventArgs e)                                      //An anonymous function which handles calling the WebClient download mehod with the appropriate link.
                {
                    try
                    {                                   // TODO: These strings could be made more flexible by using a placeholder, simliar to ParameterizedQueryStrings for SQL
                        Uri downloadLink = null;                                                //A Uri that will hold the download link.
                        switch (entry.RegionCode)                                               //Switch on the region and download using the appropriate link (necessary since theotherzone.com/wii uses separate folders for each region)
                        {                                               
                            case WbfsEntry.RegionCodes.NTSC:
                                downloadLink = new Uri(Properties.Settings.Default.CoverLinkNTSC + entry.EntryID + ".png");
                                break;
                            case WbfsEntry.RegionCodes.NTSCJ:                                       
                                downloadLink = new Uri(Properties.Settings.Default.CoverLinkNTSCJ + entry.EntryID + ".png");
                                break;
                            case WbfsEntry.RegionCodes.PAL:
                                downloadLink = new Uri(Properties.Settings.Default.CoverLinkPAL + entry.EntryID + ".png");
                                break;
                            case WbfsEntry.RegionCodes.KOR:
                                downloadLink = new Uri(Properties.Settings.Default.CoverLinkKOR + entry.EntryID + ".png");
                                break;
                            case WbfsEntry.RegionCodes.NOREGION:
                                downloadLink = new Uri(Properties.Settings.Default.CoverLinkNTSC + entry.EntryID + ".png");
                                break;
                            default:
                                downloadLink = new Uri(Properties.Settings.Default.CoverLinkNTSC + entry.EntryID + ".png");
                                break;
                        }
                        wc.DownloadFileAsync(downloadLink, fullFilename, entry);                //Make the asynchronous download call, pass the entry as a parameter so that the CoverImageLocation can be updated.
                        return;
                    }
                    catch (Exception exc)
                    {
#if DEBUG
                        System.Windows.MessageBox.Show("Error downloading cover file: " + exc);     //print the exception if an exception occurs here.
#endif
                    }
                });
            bw.RunWorkerAsync();                                                                //Invoke the background worker
            return WbfsEntry.COVERDOWNLOADING;                                                  //Return WbfsEntry.COVERDOWNLOADING as the path (which is ":COVERDOWNLOADING:" and can't be mistaken for a real path). 
        }                                                                                       //The UI uses this to show the download in progress placeholder cover.

        /// <summary>
        /// Event handler for when the cover download is finished. Sets the entry's CoverImageLocation to the path where the image was downloaded.
        /// If the download failed for some reason, it sets the CoverImageLocation to WbfsEntry.NOCOVER.
        /// </summary>
        private void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            WbfsEntry entry = (WbfsEntry)e.UserState;                                           //Retrieve the WbfsEntry object that this cover belongs to from the parameters.
            if (e.Error != null)                                                                //If the error status is set, an error occurred, so give up and set the cover location to NOCOVER.
            {
                entry.CoverImageLocation = WbfsEntry.NOCOVER;
                return;
            }
            String fullFilename = IOPath.Combine(GetMyDocsCoverFullPath(), entry.EntryID + ".png");     //Make sure the file exists in the My Docs\WBFS Manager Covers directory
            if (!File.Exists(fullFilename))                                                             //If it doesn't exist, something went wrong with the download, so set the cover location to NOCOVER.
            {
                entry.CoverImageLocation = WbfsEntry.NOCOVER;
                return;
            }
            entry.CoverImageLocation = fullFilename;                                                    //Otherwise the download was successful, so set the CoverImageLocation to the path we downloaded it to.
        }

        /// <summary>
        /// Returns a string which is the full path to the My Documents\WBFS Manager Covers for the current user.
        /// </summary>
        /// <returns>A string which is the full path to the My Documents\WBFS Manager Covers for the current user.</returns>
        private string GetMyDocsCoverFullPath()
        {       //Get the full path to the My Docs folder and combine it with the name of the default cover directory (i.e. WBFS Manager Covers)
            return IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Properties.Settings.Default.MyDocsCoverDir);
        }

        /// <summary>
        /// A method which does the RAR extraction process and reports progress or any errors that may have occured using the passed in callbacks.
        /// The callbacks can be used to resolve issues like missing volumes or password protected archives.
        /// </summary>
        /// <param name="filename">The full path to the RAR file to be extracted.</param>
        /// <param name="unrar_ExtractionProgress">The event handler (callback) which is used to report progress.</param>
        /// <param name="unrar_MissingVolume">The event handler (callback) which is used to report a missing volume. This callback can be used to resolve this issue by prompting the user.</param>
        /// <param name="unrar_PasswordRequired">The event handler (callback) which is used to report a password protected archive. This callback can be used to resolve this issue by prompting the user.</param>
        /// <returns>Returns a list of filename strings of ISO files that were extracted. Returns null if no ISO files were extracted.</returns>
        private List<String> ExtractRar(String filename, ExtractionProgressHandler unrar_ExtractionProgress, MissingVolumeHandler unrar_MissingVolume, PasswordRequiredHandler unrar_PasswordRequired)
        {
            Unrar unrar = null;                                         //An instance of the Unrar class that will do the unraring.
            List<String> extractedIsoFilenames = null;                  //The list of extracted ISO filenames.
            try
            {
                unrar = new Unrar();                                    //initialize the Unrar object
                unrar.Open(filename, Unrar.OpenMode.List);              //Have it open the RAR file in listing mode.
                string[] fileList = unrar.ListFiles();                  //Get the list of files in the archive
                bool hasIsos = false;                                   //Flag indicating whether or not the current archive actually has any ISO files in it.
                foreach (String item in fileList)                       //Iterate over the files in the RAR archive and if any of the have a .iso extension, set the hasIsos flag to true.
                {
                    if (item.Contains(".iso"))
                    {
                        hasIsos = true;
                        break;                                          //break out as soon as we've found the first .iso file.
                    }
                }
                unrar.Close();                                          //Close the archive.
                if (!hasIsos)                                           //If no ISO files have been found, return null
                    return null;

                unrar = new Unrar();                                    //Create a new instance of the Unrar object.
                extractedIsoFilenames = new List<String>();             //Instantiate the list of extracted ISO files.
                unrar.ExtractionProgress += unrar_ExtractionProgress;   //Hookup the passed in event handlers
                unrar.MissingVolume += unrar_MissingVolume;
                unrar.PasswordRequired += unrar_PasswordRequired;
                unrar.DestinationPath = TempDirectory;                  //Tell it to unrar the files in the TempDirectory specified by the user
                unrar.Open(filename, Unrar.OpenMode.Extract);           //Open the RAR file in extract mode.
                while (unrar.ReadHeader())                              //While theres still something to read
                {
                    if (unrar.CurrentFile.FileName.Contains(".iso"))    //check if the current file is an iso file
                    {
                        ExtractionProgressEventArgs progressZero = new ExtractionProgressEventArgs {PercentComplete = 0};       //if so, call the progress callback to set it to zero.
                        unrar_ExtractionProgress(null, progressZero);
                        unrar.Extract();                                                                    //Call the extract function to do the actual extraction
                        extractedIsoFilenames.Add(Path.Combine(TempDirectory, unrar.CurrentFile.FileName)); //Once the extraction is successfully done, add the extracted ISO file to the list to be returned
                    }
                    else
                    {
                        unrar.Skip();                                   //If the current file isn't an .iso file, skip it, we're not interested in it.
                    }
                }
            }
            catch (Exception e)                                         //if an exception occurs, close the RAR file and throw the exception.
            {
                if (unrar != null)
                {
                    unrar.Close();
                }
                throw e;                                                // TODO: Replace with a proper error message...
            }
            if (unrar != null)                                          //If the extraction process is successful and the unrar object isn't null, close the RAR archive.
            {
                unrar.Close();
            }
            return extractedIsoFilenames;                               // return the list of extracted ISO files.
        }

        /// <summary>
        /// A method intended to the same thing ExtractRAR does on Zip files.
        /// Not yet implemented.
        /// </summary>
        private void ExtractZip()
        {
            throw new NotImplementedException();                        //Not implemented, so throw an exception if anyone tries to call it so they know
        }

        /// <summary>
        /// A method that will allow directly adding the arhcive to the WBFS drive, bypassing the EntriesToAdd list, in case the user wants to do an "unattended install".
        /// Not yet implemented.
        /// </summary>
        private void AddArchive()
        {
            throw new NotImplementedException();                        //Not implemented, so throw an exception if anyone tries to call it so they know
        }
        #endregion

        #region INotifyPropertyChanged Members

        /// <summary>
        /// Tells the UI to re-check the named property and reflect any changes in the UI
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
//WbfsIntermWrapper.SubscribeErrorEvent(fatalErrorCallback);        //Causes problems because it gets GCed