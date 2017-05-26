using System;
using System.Windows;
using System.Windows.Documents;

namespace WBFSManager
{
    /// <summary>
    /// Interaction logic for UpdateViewer.xaml
    /// </summary>
    public partial class UpdateViewer : Window
    {
        FlowDocument _document = null;
        public UpdateViewer(String title, FlowDocument document)
        {
            InitializeComponent();
            this.Title = title;
            _document = document;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_document == null)
                return;
            updateFrame.Navigate(_document);
            updateFrame.NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden;
        }
    }
}
