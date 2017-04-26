using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace CryptedFileDecrypter
{
    public partial class KeyCalculationInfoPage : Page
    {
        public KeyCalculationInfoPage()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // We don't want to open the link in the WPF WebView. Instead, open the hyperlink in the user's browser.
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void NavToKeyCalculationPage(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new KeyCalculationPage());
        }
    }
}
