using CryptedFileDecrypter.ViewModels;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CryptedFileDecrypter
{
    public partial class DecryptionPage : Page
    {
        public DecryptionPage()
        {
            InitializeComponent();

            DataContext = ViewModel = new DecryptionPageVM();
        }

        public DecryptionPageVM ViewModel { get; private set; }

        private void FileDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            ViewModel.DecryptFiles(files);
        }
        
        private void ShowFilePickerDialog(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Multiselect = true,
                Filter = _cryptedFileFilter
            };

            bool? result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                ViewModel.DecryptFiles(dialog.FileNames);
            }
        }

        private void ShowFolderPickerDialog(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                Multiselect = true
            };

            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                // The .crypted file lookup can take a while depending on the size of the directory. 
                // Do this on a background thread so the UI thread doesn't appear to hang.
                var task = Task.Run(() =>
                {
                    // Iterate through all the folders that the user selected and find all the .crypted files in those folders.
                    // Pass the list of .crypted files to the VM for decryption.
                    List<FileInfo> cryptedFiles = new List<FileInfo>();
                    foreach (string dirPath in dialog.FileNames)
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
                        cryptedFiles.AddRange(dirInfo.GetFiles("*.crypted", SearchOption.AllDirectories));
                    }

                    ViewModel.DecryptFiles(cryptedFiles.ToArray());
                });
            }
        }

        private static readonly string _cryptedFileFilter = "Encrypted files (*.crypted)|*.crypted";
    }
}
