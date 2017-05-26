using System;
using System.Windows;
using System.Reflection;

namespace WBFSManager
{
    /// <summary>
    /// The usual App.cs, rewritten to include a catch all exception handler that asks the user to forward the exception details to the developer.
    /// </summary>
    public class AppMain
    {
        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public static void Main()
        {
            try
            {
                SplashScreen splashScreen = new SplashScreen("content/wbfsmanager%20bannerNew.png");
                splashScreen.Show(true);
                WBFSManager.App app = new WBFSManager.App();
                app.InitializeComponent();
                app.Run();
            }
            catch (BadImageFormatException)
            {
                MessageBox.Show(Properties.Resources.ErrorIncorrectVersionStr, Properties.Resources.ErrorIncorrectVersionShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            catch (Exception e)
            {
                try
                {
                    System.IO.StreamWriter sw = new System.IO.StreamWriter(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "log.txt"), true);
                    if (e.InnerException != null)
                    {
                        sw.WriteLine("Fatal error occurred. Please send the following information to the application developer at wbfsmanager@gmail.com (You can copy this text by pressing Ctrl+C): " +
                             "Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + ", OS: " + Environment.OSVersion.VersionString + " Message: " + e.Message + " \nInnerException: " + e.InnerException.Message
                                 + " \nStack trace: " + e.StackTrace);
                    }
                    else
                    {
                        sw.WriteLine("Fatal error occurred. Please send the following information to the application developer at wbfsmanager@gmail.com (You can copy this text by pressing Ctrl+C): " +
                          "Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + ", OS: " + Environment.OSVersion.VersionString + " Message: " + e.Message
                              + " \nStack trace: " + e.StackTrace);
                    }
                    sw.Flush();
                    sw.Close();
                }
                catch
                {
                }
                if (e.GetType() == typeof(BadImageFormatException))
                {
                    MessageBox.Show("Fatal error occurred. You are using the wrong version of this software for your operating system. If you are using Windows 64-bit, please download the 64-bit version of WBFS Manager.", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (e.InnerException != null)
                {
                    MessageBox.Show("Fatal error occurred. Please send the following information to the application developer at wbfsmanager@gmail.com (You can copy this text by pressing Ctrl+C or use the log file in the installation directory (must Run as Administrator in Vista+)): " +
                      "Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + ", OS: " + Environment.OSVersion.VersionString + " Message: " + e.Message + " \nInnerException: " + e.InnerException.Message
                          + " \nStack trace: " + e.StackTrace, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Fatal error occurred. Please send the following information to the application developer at wbfsmanager@gmail.com (You can copy this text by pressing Ctrl+C or use the log file in the installation directory (must Run as Administrator in Vista+)): " +
                      "Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + ", OS: " + Environment.OSVersion.VersionString + " Message: " + e.Message
                          + " \nStack trace: " + e.StackTrace, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            
        }
    }
}
