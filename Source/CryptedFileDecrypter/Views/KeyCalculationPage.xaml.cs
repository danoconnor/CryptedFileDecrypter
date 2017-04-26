using CryptedFileDecrypter.ViewModels;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace CryptedFileDecrypter
{
    public partial class KeyCalculationPage : Page
    {
        public KeyCalculationPage()
        {
            InitializeComponent();

            DataContext = ViewModel = new KeyCalculationPageVM();
        }

        public KeyCalculationPageVM ViewModel { get; private set; }

        private void UnencryptedFilePanel_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                // If the user dragged more than one file, only use the first one.
                ViewModel.UnencryptedFileInfo = new FileInfo(files[0]);
            }
        }

        private void CryptedFilePanel_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                // If the user dragged more than one file, only use the first one.
                ViewModel.EncryptedFileInfo = new FileInfo(files[0]);
            }
        }

        private void UnencryptedBrowseButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Multiselect = false
            };
            bool? result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                ViewModel.UnencryptedFileInfo = new FileInfo(dialog.FileName);
            }
        }

        private void EncryptedBrowseButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Multiselect = false
            };
            bool? result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                ViewModel.EncryptedFileInfo = new FileInfo(dialog.FileName);
            }
        }

        private void ContinueButtonClick(object sender, RoutedEventArgs e)
        {
            // Save any key changes to disk
            EncryptionSingleton.Instance.SaveDecryptionInfo();
            NavigationService.Navigate(new DecryptionPage());
        }

        /// <summary>
        /// Whenever a file name TextBox's text changes, scroll to the end of the TextBox so that the filename is visible even if the path is longer than the width of the TextBox.
        /// </summary>
        private void FileNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            Debug.Assert(textBox != null);

            textBox.CaretIndex = textBox.Text.Length;
            Rect rect = textBox.GetRectFromCharacterIndex(textBox.CaretIndex);
            textBox.ScrollToHorizontalOffset(rect.Right);
        }
    }
}
