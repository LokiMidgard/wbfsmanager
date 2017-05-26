using System;
using System.IO;
using System.Reflection;
using RssUpdater;

namespace WBFSManager
{
    public static class Utils
    {
        #region Static Methods
        /// <summary>
        /// Compares the version of the executing assembly with the passed in version object and returns -1 if the current
        /// version is newer, 0 if the versions are equal and 1 if the passed in version is newer than the executing assembly.
        /// </summary>
        /// <param name="otherVersion"></param>
        /// <returns></returns>
        public static int CompareVersions(Version otherVersion)
        {
            return otherVersion.CompareTo(Assembly.GetExecutingAssembly().GetName().Version);
        }
        /// <summary>
        /// Checks if the host OS is Windows XP.
        /// Used when closing the drive as WinXP has somewhat different behavior than Vista+.
        /// </summary>
        /// <returns></returns>
        public static bool CheckForWindowsXP()
        {
            OperatingSystem osinfo = Environment.OSVersion;
            if (osinfo.Platform == PlatformID.Win32NT && osinfo.Version.Major == 5) //Windows NT version 5 = Win XP and 2003
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks the user specified temp folder to make sure theres enough space for a single layer DVD
        /// </summary>
        /// <param name="tempFolder"></param>
        /// <returns></returns>
        public static bool CheckTempForSpace(String tempFolder)
        {
            DriveInfo di = new DriveInfo(tempFolder);
            if (di.AvailableFreeSpace < (4.5 * 1024 * 1024 * 1024))
                return false;
            return true;
        }

        /// <summary>
        /// Checks for a newer version of the application by reading the RSS feed from the update blog and comparing the version on the blog with the version
        /// number of the executing assembly. If there's a newer version it converts the HTML into a XAML FlowDocument and passes it back out, otherwise it returns null.
        /// </summary>
        /// <returns></returns>
        public static String CheckForNewerVersion()
        {
            try
            {
                RssReader rssReader = new RssReader();          //Read the RSS feed
                RssFeed rssFeed = rssReader.Retrieve(Properties.Settings.Default.UpdateLink);
                if (rssFeed.Items.Count > 0)                    //If theres at least one item, grab the item off the top (the newest one)
                {
                    Version v = new Version(rssFeed.Items[0].Title);        //Convert the title into a version string and compare it with the current assembly's version
                    if (CompareVersions(v) > 0)
                    {
                        return HTMLConverter.HtmlToXamlConverter.ConvertHtmlToXaml(rssFeed.Items[0].Description, true);     //if its newer convert the description into a XAML FlowDocument
                    }
                }
            }
            catch (Exception exc)
            { 
#if DEBUG
                throw exc;
#endif
            }
            return String.Empty;
        }
        #endregion
    }
}
