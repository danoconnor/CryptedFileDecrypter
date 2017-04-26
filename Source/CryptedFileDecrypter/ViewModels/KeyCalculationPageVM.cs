using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptedFileDecrypter.ViewModels
{
    public class KeyCalculationPageVM : INotifyPropertyChanged
    {
        public KeyCalculationPageVM()
        {
            _unencrytpedFileInfo = null;
            _unencrytpedFileInfo = null;

            ShowKeyErrorIcon = false;

            ShowUnencryptedFileErrorIcon = false;
            ShowUnencryptedFileCheckIcon = false;
            UnencryptedFileErrorMessage = string.Empty;

            ShowEncryptedFileErrorIcon = false;
            ShowEncryptedFileCheckIcon = false;
            EncryptedFileErrorMessage = string.Empty;

            ContinueButtonEnabled = false;
            KeyMessage = string.Empty;

            // Pre-populate this page if we have a saved key from a previous session.
            if (EncryptionSingleton.Instance.LoadedDecryptionInfoFromSave)
            {
                // The original unencrypted backup file and encrypted comparison files may have been moved, deleted, or corrupted by the virus.
                // We still have the key, so don't treat this as a failure state. Just be prepared for the situation.

                FileInfo unencryptedFileInfo = new FileInfo(EncryptionSingleton.Instance.UnencryptedFilePath);
                if (unencryptedFileInfo.Exists)
                {
                    UnencryptedFileInfo = unencryptedFileInfo;
                }

                FileInfo encryptedFileInfo = new FileInfo(EncryptionSingleton.Instance.EncryptedFilePath);
                if (encryptedFileInfo.Exists)
                {
                    EncryptedFileInfo = encryptedFileInfo;
                }

                // The best validation we can do on the key is that it has a length greater than 0
                // The EncryptionSingleton has already done that validation while loading the save data,
                // so here we can assume that the key is valid.
                KeyMessage = $"Success! The key has been loaded from a previous session and has length {EncryptionSingleton.Instance.EncryptionKey.Length}.";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool ContinueButtonEnabled { get; private set; }

        /// <summary>
        /// The setter will only set if the unencrypted file is valid or not, but the getter will return whether 
        /// the unencrypted file is valid and there is no key calculation error.
        /// </summary>
        public bool ShowUnencryptedFileCheckIcon
        {
            get
            {
                return _showUnencryptedFileCheckIcon && !ShowKeyErrorIcon;
            }

            private set
            {
                _showUnencryptedFileCheckIcon = value;
            }
        }
        private bool _showUnencryptedFileCheckIcon;

        public bool ShowUnencryptedFileErrorIcon { get; private set; }

        /// <summary>
        /// The setter will only set if the encrypted file is valid or not, but the getter will return whether 
        /// the encrypted file is valid and there is no key calculation error.
        /// </summary>
        public bool ShowEncryptedFileCheckIcon
        {
            get
            {
                return _showEncryptedFileCheckIcon && !ShowKeyErrorIcon;
            }

            private set
            {
                _showEncryptedFileCheckIcon = value;
            }
        }
        private bool _showEncryptedFileCheckIcon;


        public bool ShowEncryptedFileErrorIcon { get; private set; }
        public bool ShowKeyErrorIcon { get; private set; }

        public string EncryptedFileErrorMessage { get; private set; }
        public string UnencryptedFileErrorMessage { get; private set; }

        public string EncryptedFilePath
        {
            get
            {
                return EncryptedFileInfo?.FullName;
            }
        }

        public string UnencryptedFilePath
        {
            get
            {
                return UnencryptedFileInfo?.FullName;
            }
        }

        /// <summary>
        /// Used to display an error or information about the calculated key.
        /// </summary>
        public string KeyMessage { get; private set; }


        private FileInfo _unencrytpedFileInfo;
        public FileInfo UnencryptedFileInfo
        {
            get
            {
                return _unencrytpedFileInfo;
            }

            set
            {
                UnencryptedFileErrorMessage = "";

                // Check to make sure the file is valid
                bool error = false;
                if (value.Extension == _cryptedExtension)
                {
                    UnencryptedFileErrorMessage = $"This file has the wrong extension. It should not end in {_cryptedExtension}.";
                    error = true;
                }
                else if (value.Length < _minFileLength)
                {
                    UnencryptedFileErrorMessage = $"The file is too small. Please select a file that is at least {_minFileLength} bytes in size";
                    error = true;
                }

                ShowUnencryptedFileCheckIcon = !error;
                ShowUnencryptedFileErrorIcon = error;

                if (!error)
                {
                    EncryptionSingleton.Instance.UnencryptedFilePath = value.FullName;
                }

                // Set the value regardless so the user can see the file name in the file name TextBox.
                _unencrytpedFileInfo = value;

                TryCalculateKey();

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UnencryptedFileErrorMessage)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowUnencryptedFileCheckIcon)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowUnencryptedFileErrorIcon)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UnencryptedFilePath)));
            }
        }

        private FileInfo _encryptedFileInfo;
        public FileInfo EncryptedFileInfo
        {
            get
            {
                return _encryptedFileInfo;
            }

            set
            {
                EncryptedFileErrorMessage = "";

                // Check to make sure the file is valid
                bool error = false;
                if (value.Extension != _cryptedExtension)
                {
                    EncryptedFileErrorMessage = $"This file has the wrong extension. It should end in {_cryptedExtension}, but it actually ends in {value.Extension}";
                    error = true;
                }
                else if (value.Length < _minFileLength)
                {
                    EncryptedFileErrorMessage = $"The file is too small. Please select a file that is at least {_minFileLength} bytes in size";
                    error = true;
                }

                ShowEncryptedFileCheckIcon = !error;
                ShowEncryptedFileErrorIcon = error;

                if (!error)
                {
                    EncryptionSingleton.Instance.EncryptedFilePath = value.FullName;
                }

                // Set the value regardless so the user can see the file name in the file name TextBox.
                _encryptedFileInfo = value;

                TryCalculateKey();

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EncryptedFileErrorMessage)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowEncryptedFileCheckIcon)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowEncryptedFileErrorIcon)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EncryptedFilePath)));
            }
        }
        
        public void TryCalculateKey()
        {
            ContinueButtonEnabled = false;
            ShowKeyErrorIcon = false;

            if (UnencryptedFileInfo == null || 
                EncryptedFileInfo == null ||
                ShowUnencryptedFileErrorIcon ||
                ShowEncryptedFileErrorIcon)
            {
                // The user has only supplied one of two files or the user has entered at least one invalid file.
                // Return with no error message because file validation error messages are set when the FileInfo object is set.
                KeyMessage = string.Empty;
            }
            else if (Math.Abs(EncryptedFileInfo.Length - UnencryptedFileInfo.Length) > 1)
            {
                KeyMessage = $"The sizes of the encrypted and unencrypted files do not match. Please select the same file in an unencrypted and encrypted form.";
            }
            else
            {
                // The encrypted file matches the unencrypted file. Try to calculate the key.

                // The virus will have encrypted either the first 1024 or 2048 bytes of each file using XOR encryption. The rest of the file is untouched.
                // First, we'll find the length of the encrypted section by comparing the encrypted and unencrypted files and figuring out where they stop being different.
                // To avoid false positives, we will require that five consecutive bytes match before we declare that we've found the lenght of the key.
                Stream encryptedFileStream = EncryptedFileInfo.OpenRead();
                byte[] encryptedFileBuff = new byte[encryptedFileStream.Length + 1];
                encryptedFileStream.Read(encryptedFileBuff, 0, (int)encryptedFileStream.Length);

                FileStream unencryptedFileStream = UnencryptedFileInfo.OpenRead();
                byte[] unencryptedFileBuff = new byte[unencryptedFileStream.Length + 1];
                unencryptedFileStream.Read(unencryptedFileBuff, 0, (int)unencryptedFileStream.Length);

                int keyLength = 0;
                for (int i = 0; i < encryptedFileBuff.Length - _numConsecutiveBytesForMatch; i++)
                {
                    bool foundMatch = true;
                    for (int byteCheckIndex = 0; byteCheckIndex < _numConsecutiveBytesForMatch; byteCheckIndex++)
                    {
                        if (encryptedFileBuff[i + byteCheckIndex] != unencryptedFileBuff[i + byteCheckIndex])
                        {
                            foundMatch = false;
                            break;
                        }
                    }

                    // If we found 5 consecutive matching bytes, then we assume that the files will continue to match from here on out. 
                    // This means that the first i bytes were encrypted and not matching.
                    if (foundMatch)
                    {
                        keyLength = i;
                        break;
                    }
                }

                // We're going to calculate the key used in the encryption by running EncryptedFile XOR UnencryptedFile = Key
                byte[] encryptionKey = new byte[keyLength];
                for (int i = 0; i < keyLength; i++)
                {
                    encryptionKey[i] = (byte)(unencryptedFileBuff[i] ^ encryptedFileBuff[i]);
                }
                
                if (keyLength > 0)
                {
                    KeyMessage = $"Success! The key is {keyLength} bytes long.";
                    ContinueButtonEnabled = true;

                    // Save off the key so other pages can use it during decryption.
                    EncryptionSingleton.Instance.EncryptionKey = encryptionKey;
                }
                else
                {
                    KeyMessage = $"Something went wrong (key length is 0). Please try choosing a different set of files.";
                }
            }

            // Show the error icon if there is an error message. If the ContinueButton is enabled, then the message is an informational message, not an error message.
            ShowKeyErrorIcon = (KeyMessage.Length > 0 && !ContinueButtonEnabled);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KeyMessage)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowKeyErrorIcon)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowEncryptedFileCheckIcon)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowUnencryptedFileCheckIcon)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ContinueButtonEnabled)));
        }

        private static readonly int _numConsecutiveBytesForMatch = 5;
        private static readonly int _minFileLength = 2048;
        private static readonly string _cryptedExtension = ".crypted";
    }
}
