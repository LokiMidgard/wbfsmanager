using System;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;

namespace WBFSManager.UI
{
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : Window
    {
        #region Fields
        private bool _infinite = true;
        private bool _useOverall = false;
        private DateTime _startTime = DateTime.Now;
        BackgroundWorker _causeWorker = null;
        #endregion
        #region Properties
        public bool ProgrammaticClose { get; set; }
        #endregion
        #region Constructor
        public ProgressDialog(String windowTitle, bool infinite, bool useOverall, int numOperations, bool showCancel, BackgroundWorker causeWorker)
        {
            InitializeComponent();
            ProgrammaticClose = false;
            Title = windowTitle;
            DetailLabel.Content = windowTitle;
            _infinite = infinite;
            ProgressBar.IsIndeterminate = _infinite;
            _useOverall = useOverall;
            
            if (_useOverall)
            {
                OverallProgGrid.Visibility = Visibility.Visible;
                OverallProgressBar.Visibility = Visibility.Visible;
                CountOverTotalLabel.Content = "(0/" + numOperations.ToString() + ")";
                //this.Cursor = Cursors.AppStarting;
            }
            if (showCancel && causeWorker != null)
            {
                _causeWorker = causeWorker;
                CancelButton.Visibility = Visibility.Visible;
            }
                
        }
        #endregion
        #region Event Handlers
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _startTime = DateTime.Now;
            
            if (_useOverall)
            {
                this.Cursor = Cursors.AppStarting;
            }
            else
                this.Cursor = Cursors.Wait;
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!ProgrammaticClose)
                e.Cancel = true;
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_causeWorker != null)
                _causeWorker.CancelAsync();
            CancelButton.IsEnabled = false;
            CancelButton.Content = Properties.Resources.ProgressCancellingText;
        }
        #endregion
        #region Methods
        public void SetProgress(double percentageCompleted)
        {
            if (_infinite)
                return;
            ProgressBar.Value = percentageCompleted;
            TimeSpan ts= DateTime.Now.Subtract(_startTime);
            ElapsedTimeValueLabel.Content = ts.Hours.ToString("00") + ":" + ts.Minutes.ToString("00") + ":" + ts.Seconds.ToString("00"); //+ "." + ts.Milliseconds.ToString("000");
        }

        public void SetProgressOverall(double percentageCompleted, int count, int total)
        {
            if (_infinite || !_useOverall)
                return;
            OverallProgressBar.Value = percentageCompleted;
            CountOverTotalLabel.Content = "(" + count.ToString() + "/" + total.ToString() + ")";

        }
        #endregion
    }
}
