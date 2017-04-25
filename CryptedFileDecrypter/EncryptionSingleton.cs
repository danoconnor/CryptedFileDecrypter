using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptedFileDecrypter
{
    public class EncryptionSingleton
    {
        public static string CryptedExtension = ".crypted";

        private static EncryptionSingleton _instance;
        public static EncryptionSingleton Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EncryptionSingleton();
                }

                return _instance;
            }
        }

        private EncryptionSingleton()
        {
            EncryptionKey = null;
            UnencryptedFilePath = string.Empty;
            EncryptedFilePath = string.Empty;
            LoadedDecryptionInfoFromSave = false;

            // See if we can find a saved encryption key (and it's source files).
            // If a saved key is found, then the KeyDecryption page will be pre-populated and the user can just continue through
            // without needing to re-choose the two files.
            if (File.Exists(_savedDecryptionKeyFilePath))
            {
                // The first two lines have the file path for the unencrypted backup file and the encrypted comparison file, respectively.
                // We use this info to populate the KeyCalculation page to inform the user what files were used to calculate the current key.
                StreamReader fileReader = new StreamReader(File.OpenRead(_savedDecryptionKeyFilePath));

                UnencryptedFilePath = fileReader.ReadLine();
                EncryptedFilePath = fileReader.ReadLine();

                // The rest of the save file is the decryption key.
                Stream fileStream = fileReader.BaseStream;
                Debug.Assert(fileStream.Position > 0);

                byte[] encryptionKey = new byte[fileStream.Length - fileStream.Position];
                int numBytesInKey = fileStream.Read(encryptionKey, 0, encryptionKey.Length);

                EncryptionKey = encryptionKey;

                fileReader.Dispose();

                // Make sure the save data was read successfully and is valid.
                if (!string.IsNullOrWhiteSpace(UnencryptedFilePath) &&
                    !string.IsNullOrWhiteSpace(EncryptedFilePath) &&
                    numBytesInKey > 0)
                {
                    LoadedDecryptionInfoFromSave = true;
                }
                else
                {
                    // This is an unexpected error state and we want to be alerted if this happens.
                    Debug.Assert(false);
                }
            }
        }

        public byte[] EncryptionKey { get; set; }

        public string UnencryptedFilePath { get; set; }

        public string EncryptedFilePath { get; set; }

        /// <summary>
        /// Returns true if the EncryptionKey, UnencryptedFilePath, and EncryptedFilePath fields have been pre-populated
        /// with save data from a previous session.
        /// </summary>
        public bool LoadedDecryptionInfoFromSave { get; private set; }

        /// <summary>
        /// Writes the contents of EncryptionKey, UnencryptedFilePath, and EncryptedFilePath to disk.
        /// These values will automatically be read from disk the next time that the singleton is initialized (next program run).
        /// </summary>
        public void SaveDecryptionInfo()
        {
            Debug.Assert(EncryptionKey != null && EncryptionKey.Length > 0);
            Debug.Assert(!string.IsNullOrWhiteSpace(UnencryptedFilePath));
            Debug.Assert(!string.IsNullOrWhiteSpace(EncryptedFilePath));

            // Ensure that the app's AppData directory exists. Otherwise, the File.Open will throw.
            FileInfo saveFileInfo = new FileInfo(_savedDecryptionKeyFilePath);
            if (!saveFileInfo.Directory.Exists)
            {
                saveFileInfo.Directory.Create();
            }

            // Create the file, overwriting whatever was previously saved
            StreamWriter outputWriter = new StreamWriter(File.Open(_savedDecryptionKeyFilePath, FileMode.Create));

            outputWriter.WriteLine(UnencryptedFilePath);
            outputWriter.WriteLine(EncryptedFilePath);

            outputWriter.Flush();

            Stream outputStream = outputWriter.BaseStream;
            Debug.Assert(outputStream.Position > 0);

            outputStream.Write(EncryptionKey, 0, EncryptionKey.Length);

            outputWriter.Flush();
            outputWriter.Dispose();
        }
        
        // It's semi-important that the file has an odd extension. The virus (according to the WebRoot analysis) tends to only 
        // encrypt certain file types that are likely to contain important user data (*.pdf, *.docx, etc.). 
        // If the virus is still active on the user's machine, then we do not want this file to get encryped so we'll give it an 
        // unusual extension that will be unlikely for the virus to include in its file search filter. 
        private static readonly string _savedDecryptionKeyFilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\CryptedFileDecrypter\\DecryptionKey.decryptioninfo";
    }
}
