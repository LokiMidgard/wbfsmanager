using System;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;

namespace WBFSManager.UI
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        #region Constants
        const string METADATAFILENAME = "metadata";
        const string LANGUAGEELEMENTNAME = "Language";
        const string CREDITSTRINGELEMENTNAME = "CreditLine";
        #endregion

        #region Constructor
        public AboutWindow()
        {
            InitializeComponent();
            AddTranslationCredits();
        }
        #endregion

        #region Event Handlers
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://www.wiinewz.com");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(@"https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=J9M77A6YJX9SQ&lc=GB&item_name=WBFS%20Manager&currency_code=CAD&bn=PP%2dDonationsBF%3abtn_donateCC_LG_global%2egif%3aNonHosted");
        }
        #endregion

        #region Methods
        private void AddTranslationCredits()
        {
            String executablePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string[] metaDataFiles = Directory.GetFiles(executablePath, METADATAFILENAME + "*.xml", SearchOption.AllDirectories);
            foreach (String metaDataFile in metaDataFiles)
            {
                try
                {
                    XElement elements = XElement.Load(metaDataFile);
                    Label creditLabel = new Label {HorizontalAlignment = HorizontalAlignment.Center, Content = elements.Element(LANGUAGEELEMENTNAME).Element(CREDITSTRINGELEMENTNAME).Value};
                    TransCredsStackPanel.Children.Add(creditLabel);
                }
                catch
                {
#if DEBUG
                    MessageBox.Show(this, "Error occurred while parsing language directories: " + metaDataFile);
#endif
                }

            }
        }
        #endregion
    }
}
