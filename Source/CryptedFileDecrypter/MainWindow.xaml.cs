using System;
using System.Windows.Navigation;

namespace CryptedFileDecrypter
{
    public partial class MainWindow : NavigationWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            NavigationService.Navigate(new InfoPage());
        }
    }
}
