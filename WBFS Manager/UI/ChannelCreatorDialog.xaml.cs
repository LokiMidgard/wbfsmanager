using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows.Media.Effects;

namespace WBFSManager.UI
{
    /// <summary>
    /// Interaction logic for ChannelCreatorDialog.xaml
    /// </summary>
    public partial class ChannelCreatorDialog : Window, INotifyPropertyChanged
    {
        public String OutputWadPath { get; set; }
        public String OutputChannelId { get; set; }
        public ChannelCreatorDialog(String genChanId)
        {
            if (!string.IsNullOrEmpty(genChanId))
            {
                OutputChannelId = genChanId;
            }
            else
                OutputChannelId = String.Empty;
            OutputWadPath = String.Empty;
            InitializeComponent();
        }

        private void BrowseOutWadButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog {Title = Properties.Resources.SaveWadFileAsStr, Filter = "WAD file (*.wad)|*.wad", ValidateNames = true, OverwritePrompt = true};
            if (!sfd.ShowDialog().GetValueOrDefault(false))
                return;
            OutputWadPath = sfd.FileName;
            NotifyPropertyChanged("OutputWadPath");
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (OutputWadPath.Trim().Length == 0)
            {
                MessageBox.Show(this, Properties.Resources.ErrorInvalidOuputWadFilenameStr, Properties.Resources.ErrorInvalidFilename, MessageBoxButton.OK, MessageBoxImage.Error);
                OutWadTextBox.ToolTip = Properties.Resources.ErrorInvalidOuputWadFilenameStr;
                DropShadowEffect effect = new DropShadowEffect {Color = Colors.Red, ShadowDepth = 0};
                OutWadTextBox.Effect = effect;
                return;
            }
            OutWadTextBox.Effect = null;
            if (OutputChannelId.Trim().Length != 4)
            {
                MessageBox.Show(this, Properties.Resources.ErrorChanIdMustBeFourStr, Properties.Resources.ErrorInvalidChanIdStr, MessageBoxButton.OK, MessageBoxImage.Error);
                ChanIDTextBox.ToolTip = Properties.Resources.ErrorChanIdMustBeFourStr;
                DropShadowEffect effect = new DropShadowEffect {Color = Colors.Red, ShadowDepth = 0};
                ChanIDTextBox.Effect = effect;
                return;
            }
            ChanIDTextBox.Effect = null;
            OutputChannelId = OutputChannelId.Trim();
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
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
    }
}
