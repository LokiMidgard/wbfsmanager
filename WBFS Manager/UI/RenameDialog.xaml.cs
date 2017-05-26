using System;
using System.Windows;

namespace WBFSManager.UI
{
    /// <summary>
    /// Interaction logic for RenameDialog.xaml
    /// </summary>
    public partial class RenameDialog : Window
    {
        #region Fields
        public String NewNameValue
        {
            get
            {
                return NewNameTextBox.Text;
            }
        }
        #endregion
        #region Constructor
        public RenameDialog(String initialValue)
        {
            InitializeComponent();
            NewNameTextBox.Text = initialValue;
            NewNameTextBox.Focus();
        }
        #endregion
        #region Event Handlers
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (NewNameValue.Trim().Length <= 0)
            {
                MessageBox.Show(this, Properties.Resources.ErrorRenameInvalidStr, Properties.Resources.ErrorRenameInvalidShortStr, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            this.DialogResult = true;
        }
        #endregion
    }
}
