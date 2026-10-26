using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TCPServer {
    public class AESq {
        private static byte[] encryptionKey = { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };

        public static string Encrypt(string connectionString) {
            using (Aes encryptor = Aes.Create()) {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes("YourPassword", encryptionKey);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);

                byte[] encryptedBytes;
                using (MemoryStream ms = new MemoryStream()) {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write)) {
                        using (StreamWriter sw = new StreamWriter(cs)) {
                            sw.Write(connectionString);
                        }
                        encryptedBytes = ms.ToArray();
                    }
                }

                return Convert.ToBase64String(encryptedBytes);
            }
        }

        public static string Decrypt(string encryptedConnectionString) {


            byte[] cipherBytes = Convert.FromBase64String(encryptedConnectionString);

            using (Aes encryptor = Aes.Create()) {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes("YourPassword", encryptionKey);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);

                string decryptedString;
                using (MemoryStream ms = new MemoryStream(cipherBytes)) {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Read)) {
                        using (StreamReader sr = new StreamReader(cs)) {
                            decryptedString = sr.ReadToEnd();
                        }
                    }
                }

                return decryptedString;
            }
        }
    }
}
