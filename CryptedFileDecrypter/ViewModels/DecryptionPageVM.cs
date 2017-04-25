using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CryptedFileDecrypter.ViewModels
{
    public class DecryptionPageVM
    {
        public DecryptionPageVM()
        {
            _decryptionKey = EncryptionSingleton.Instance.EncryptionKey;
            Debug.Assert(_decryptionKey != null && _decryptionKey.Length > 0);

            Messages = new ObservableCollection<string>();

            _isCurrentlyDecrypting = false;
            _currentlyDecryptingLock = new object();
            _filesWaitingToDecrypt = new List<FileInfo>();
        }

        public ObservableCollection<string> Messages { get; private set; }

        public void DecryptFiles(string[] files)
        {
            FileInfo[] fileInfos = new FileInfo[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                fileInfos[i] = new FileInfo(files[i]);
            }

            DecryptFiles(fileInfos);
        }

        public void DecryptFiles(FileInfo[] files)
        {
            // Decryption is generally very fast, but large batches may take some time.
            // Run the decryption on a background thread, to avoid hanging the UI thread.
            //
            // If the user tries to decrypt more files while a decryption process is running,
            // then we will add those files to a queue to be decrypted when the initial batch finishes.
            if (!_isCurrentlyDecrypting)
            {
                _isCurrentlyDecrypting = true;
                var task = Task.Run(() =>
                {
                    foreach (FileInfo file in files)
                    {
                        DecryptFile(file);
                    }
                }).ContinueWith(innerTask =>
                {
                    _isCurrentlyDecrypting = false;

                    // Begin decryption of any files that were added to the queue while the background thread was running
                    lock (_currentlyDecryptingLock)
                    {
                        if (_filesWaitingToDecrypt.Count > 0)
                        {
                            DecryptFiles(_filesWaitingToDecrypt.ToArray());
                            _filesWaitingToDecrypt.Clear();
                        }
                    }
                });
            }
            else
            {
                lock (_currentlyDecryptingLock)
                {
                    _filesWaitingToDecrypt.AddRange(files);
                }
            }
        }

        private void DecryptFile(FileInfo encryptedFile)
        {
            // Ignore any files that are do not have the .crypted extension.
            if (encryptedFile.Extension != _cryptedExtension)
            {
                return;
            }

            FileStream inputStream = encryptedFile.OpenRead();
            byte[] inputBuff = new byte[inputStream.Length + 1];
            inputStream.Read(inputBuff, 0, (int)inputStream.Length);

            // The length of the key is the length of the encrypted portion of the file (the rest of the file is untouched).
            // While we are iterating through the encrypted portion, the formula is outputByte = encryptedByte XOR keyByte
            // After we are done with the encrypted section, it will just be outputByte = encryptedByte. At this point the "encryptedByte" is not actually encrypted and is the same byte that the output file should have.
            byte[] outputBuff = new byte[inputBuff.Length];
            for (int i = 0; i < outputBuff.Length; i++)
            {
                if (i < _decryptionKey.Length)
                {
                    outputBuff[i] = (byte)(inputBuff[i] ^ _decryptionKey[i]);
                }
                else
                {
                    outputBuff[i] = inputBuff[i];
                }
            }

            // Write the decrypted file to a new file with the same name
            string encryptedFileName = encryptedFile.Name;
            string outputFileName = encryptedFileName.Remove(encryptedFileName.IndexOf(_cryptedExtension));
            string outputFilePath = encryptedFile.Directory.FullName + "\\" + outputFileName;

            // We don't want to overwrite existing data, so make sure there is no name conflict.
            // If a name conflict is encountered, then we will append a (Copy [#]) to the front of the filename until there is no more name conflict.
            int numDuplicateFileNames = 0;
            while (File.Exists(outputFilePath))
            {
                numDuplicateFileNames++;
                outputFilePath = $"{encryptedFile.Directory.FullName}\\(Copy {numDuplicateFileNames}) - {outputFileName}";
            }

            FileStream outputStream = File.Create(outputFilePath);
            outputStream.Write(outputBuff, 0, outputBuff.Length);
            outputStream.Flush();
            outputStream.Dispose();

            // Need to run the Messages.Add on the UI thread (since it is an ObservableCollection and is notifying UI thread XAML controls)
            App.Current.Dispatcher.Invoke(() => { Messages.Add($"Decrypted {encryptedFile.FullName}{Environment.NewLine}    Saved as {outputFilePath}"); });
        }

        private byte[] _decryptionKey;

        private bool _isCurrentlyDecrypting;
        private object _currentlyDecryptingLock;
        private List<FileInfo> _filesWaitingToDecrypt;

        private static readonly string _cryptedExtension = ".crypted";
    }
}
