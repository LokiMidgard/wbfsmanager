Wii Backup File System Manger 3.0 (WBFS Manager 3.0)
Written by AlexDP

First off, I'd like to thank the few people who generously donated their hard-earned money, no matter how much the amount was. 
Second, I'd like to thank all the translators who put in their own precious time to help with the translations and BarbaxX for the new imagery.

This is the last major update to WBFS Manager, the following fixes and changes have been made:

Changes (3.0):
-Added Channel Creation support. (Disabled by default, to enable go to Options->Channel Creation and Enable. Follow the instructions provided below.)
-Added "Large Cover View"s, which are two side panels that display the cover for the currently selected item.
-Added Help menu option.
-Added Automatically Check for Updates option (Requires Internet access, can be disabled).
-Added the option to cancel batch operations (cancellation occurs as soon as the current item is finished).
-Added drive status indicator under the left-hand list, blue shows amount of used space, green shows amount of used space after adding all entries from the "Games To Add" list.

Fixes (3.0):
-Fixed the estimated size being wrong for some games.
-Fixed an issue with CSV files being exported incorrectly sometimes.
-Save expander states, so the user doesn't have to keep re-opening them each time.
-Fixed Homebrew Channel creation. (Now using WiiCrazy's USB Loader DOL instead of Yal).
-Fixed an issue with the "Estimated Total Size" not updating correctly when games were removed from the right-hand list.
-Now checks the filesystem type of a drive before loading to make sure it's not NTFS, FAT or ext.
-Fixed an issue with games with ":" or other special characters causing issues during batch extraction.
-Fixed issue with game regions showing up incorrectly in initial 2.5 release.
-Fixed issue with RAR files in subfolders not being detected during drag and drop operations.
-Removed thousandth of a seconds from elapsed time in progress bar.
-Fixed mislabled blocks status (was Blocks Used, should be Blocks Free).
-Fixed some other minor issues.
-Changed installer (now prompts for .NET 3.5 SP1 before installing, also gives options for installing langauges, etc).

Channel Creation notes:
-To enable channel creation, go to Edit->Options and click on the Channel Creation tab. Then specify the base WAD file that you want to use, the location of the common-key.bin file, as well as the loader that you want to use. Click OK to confirm the changes.
-Once channel creation has been enabled, you can choose one or more entries on the Games To Add list then click "Create Channels" (or right click and choose "Create Channels").
	-If you choose multiple entries, you will be asked to choose a folder to save the WAD files in and a batch creation process will begin.
	-If you choose a single entry, you will be prompted for the 4 letter Channel ID you want to use and where you want to save the file.
		-Be careful with the Channel IDs you choose, if you have an existing channel with the same ID, it will be overwritten when you install the channel.
-If you want, you can specify a USB loader DOL file other than the packaged presets. To do this, fill in the "USB Loader DOL File" setting with the DOL file you want to use, then type in the placeholder title ID in the "Title ID Placeholder" field, then click OK to confirm the changes.
-A base WAD is not included with this application, you must supply your own. It is best to use a WAD file that belongs to a channel and one that is the same region as your console.
-The common-key.bin file is not included with this application, you must supply the common-key.bin file.

Changes (2.5):
-Added indirect Drive-To-Drive transferring and cloning. (Click Drive-To-Drive expander (the line under the buttons) to show related options).
-Added automatic RAR archive extraction (drag and drop or use the browse button).
-Added batch extraction and deletion. (Hold shift to select a range of items, or hold Ctrl to select separate individual items).
-Added exporting list of games on drive to a .CSV (Comma-separated values) file. Right-click on left-hand list and click export. Can export entire list or only selected items.
-Added ability to use more than one cover directory (all downloaded covers will still be downloaded to My Documents\WBFS Manager Covers)
-Added estimated total size for Games to Add list.
-Added Italian, German and Chinese (Traditional) language support
-Added information about the number of items in each list.
-Added message informing user that they're using the wrong version if they try using 32-bit version on 64-bit Windows.
-Updated some icons and imagery (courtesy of BarbaxX).

Fixes (2.5):
-Fixed an issue with some buttons being hidden in some languages. (Now using a toolbar. Click on arrow on right side of toolbar to see any hidden icons).
-Fixed a bug with the 2.2.2 not working on Chinese OSes (released earlier as Chinese Edition).
-Fixed some special cases for region codes.
-Fixed some issues with some translations.
-Fixed a few other minor bugs.

Indirect Drive-To-Drive transferring and Cloning/Auto RAR Extraction notes:
In this version drive-to-drive transferring and Auto RAR extraction are done using an intermediary temporary directory that is set when you first launch WBFS Manager.
The drive with the temporary directory on it should have at least 4.8 GB free space for single layer games. Double-layer games will require more space.
You can change the temporary directory by going to Edit->Options.
To do a drive-to-drive transfer:
|	1.Load the source drive.
|	2.Open the Drive-To-Drive section and load the target (secondary) drive.
|	3.Click "Clone" to copy all games from the source drive to the target drive.
|	OR
|	Choose some games from the left-hand list (source drive) using Shift or Ctrl, then click "Copy to this Drive".

Changes (2.2.2):
-Improved multilingual support (can now add more languages by dropping "language packs" into the application's directory).
-Added Dutch support.

Fixes (2.2.1):
-Fixed the libwbfs delete bug.

Changes (2.2.1):
-Automatic cover downloading (You will be prompted the first time you run the program if you want to enable or disable this. You can also change this setting later from the Edit->Download Covers from Web menu). Please be a bit patient, it takes a couple of seconds to download as the site is somewhat slow. While it's downloading you'll see a placeholder image which will be automatically updated once the download is complete.
-Added Region code information
-Changed default location for covers to My Documents\WBFS Manager Covers for better UAC compliance.
-Added functionality to help debug the "crash on startup" situations, namely what tommyv and kruy are facing.
-Added Spanish language support, coutesy of dgtor. 

Fixes (2.2):
-Kept error messages from appearing when adding multiple files. If an error occurs, the entry will be marked with a red error symbol in the Games to Add list and a tooltip with the error message will appear if you hover the mouse over the icon.

Changes (2.2):
-Added multilingual support (currently only French, courtesy of TheCrach), contact me if you'd like to add support for your language.
-Added Hombrew Channel creation (not completely tested).
-Added Cover support. Hover mouse over an entry to see the cover.
-Added support for automatically downloading covers. You will be prompted on the first run to enable or disable it. You can also change this from the Edit->Download Covers from Web menu.
-Now defaults the selected drive in the drive combo box to the last used drive.
-Added region code information.
-Item counts for both lists.
-Added Check for Updates to Help menu. Currently only links to WBFS Manager blog which will list updates. Plan to implement proper updater later to help everyone keep up to date.

Cover file instructions:
Copy covers into the My Documents\WBFS Manager Covers directory to show them in WBFS Manager
OR
Set your own cover directories from the Edit Menu in WBFS Manager (Edit->Set Covers Directory...).
All downloaded covers will always be saved in My Documents\WBFS Manager Covers.
Covers can be disabled by unchecking the Show Covers option (Edit->Show Covers).
Covers can be in subdirectories.
Cover files must be named as follows: DISCID.png where DISCID is the 6 letter disc ID for a given game. For example, Wii Sports cover would be: RSPE01.png
If you use set your own cover directory, that directory will be checked first, then this directory.

Multilingual support details:
WBFS Manager will automatically use the language of your operating system if it is one of the supported languages. If you'd like to manually change the language you can do so from the Edit->Language menu.
Contact me at wbfsmanager { at } gmail.com if you'd like to add support for your language to WBFS Manager.

Fixes (2.1):
-No black screen while adding or extracting. Application remains responsive and reports progress.
-Fixed issue with size information being covered by scroll bars.
-A few other minor fixes.

Changes (2.1):
-Added rename functionality (thanks to Sorg).
-Application now directly uses libwbfs, rather than using wbfs_win.exe.
-Added icons for better usability.
-Added list sorting functionality (right click on each list for the different sort options.
-Added blocks used to status bar.
-Added visual indicators to "Games to add" list. Check mark if the iso was copied successfully, red error symbol if the copying the iso failed.
-Changed "size" label to "estimated size" in the "Games to add" list. Added iso size information. Reported estimated sizes are still estimates and may be significantly different from actual size on WBFS drive. Sizes on the "Games on WBFS drive" list are accurate.
-Added first time welcome screen with basic instructions.
-Added "Refresh Drive List" button in case a USB drive was not plugged in when the program started. Clicking Refresh Drive List should show USB drives that were plugged in after WBFS Manager started.
-Added support for dropping a folder instead of one or more iso files. All iso files in the dropped folder and its subdirectories will automatically be added.
-Added progress bars and an activity indicator to indicate the program is running and has not crashed to Windows XP users.

Note: There is no shared code with the other wbfs_win GUI apps that have been released. 
The wbfs_win.exe from the wbfs_win_delta release is also no longer being used by this application. It now uses a cleaner, less hacky way of using the libwbfs source code for direct manipulation.

Note 2: Although I've tested this a good deal it's only the first release and it may have some bugs. If you notice any let me know. For the most part everything should be ok as the actual work with the ISOs and WBFS drive are done in libwbfs. Of course, pulling your USB cable out while copying files isn't going to help.

Requirements:
-.NET Framework 3.5 SP1 (the installer will prompt you to automatically download and install it if you don't already have it.

Supported Operating Systems:
-Windows Vista (tested thoroughly. May need to "Run as Administrator" depending on user's settings and UAC settings).
-Windows 7 (tested and working. May need to "Run as Administrator" depending on user's settings and UAC settings).
-Windows XP (tested by a few other users, should work fine.)

What it does:
This application basically provides a GUI for the command line-based wbfs-win, used for accessing legal Wii Backups stored on disk drives that have been formatted to the WBFS file system.
It provides all the basic functionality that comes with wbfs-win, including formatting to wbfs, adding backups, getting a listing of backups already on the drive, extracting ISOs from the WBFS drive, etc.

Features:
-Listing of games with titles, sizes and codes.
-Drag-and-drop support for adding multiple files at once to the WBFS drive.
-Easy to use interface which also reports available, total and used disk space at a glance.
-Batch processing of multiple ISOs.
-Rename discs on the WBFS drive.
-Batch extract and delete.
-Indirect drive-to-drive copying and cloning.
-Automatic RAR extraction.
-Export list of games to CSV file.

Instructions:
-Install using setup.
-Plug in the hard drive or USB stick.
-Run the application.
-Choose the correct drive letter.
-Click Load. (If you haven't already formatted the disk to WBFS, you can do that by clicking Format).
-You should now see any backups on the drive on the left hand side. 
-You can drag and drop ISO files from Windows Explorer onto the right hand side or you can click the browse button.
-Click the Add to Drive button to copy them over to the WBFS drive.
-Enjoy!

Completed Future Plans:
-Using libwbfs directly to achieve greater flexiblity.
-Use a separate worker thread to improve responsiveness when doing IO operations.
-UnRar functionality to automatically UnRar files and add the iso to the WBFS drive.

Remaining Future Plans:
-ISO library
-Additional info from ISO (Game image shown in Disc Channel,...)

DISCLAIMER (Borrowed from Waninkoko, hopefully he won't mind :) )
  THIS APPLICATION COMES WITH NO WARRANTY AT ALL, NEITHER EXPRESS NOR IMPLIED.
  I DO NOT TAKE ANY RESPONSIBILITY FOR ANY DAMAGE TO YOUR WII CONSOLE
  BECAUSE OF IMPROPER USAGE OF THIS SOFTWARE.

Thanks to Kwiirk for the wbfs tool, Sorg for the rename disc functionality, Waninkoko for a good deal of things and Team Twiizers. 
Thanks to TheCrach for the French translations, BarbaxX for the German, dgtor for the Spanish, cerocca for the Italian and Perugino, villadelfia for the Dutch and IvanChen for the Chinese translations.
Thanks to BarbaxX for addtional icon and imagery work.
The UnRar DLL library is used in this application for UnRaring archives.
Hopefully I've given everyone thier due credit. If you feel you deserve credit for a portion of the code used in this application please let me know.