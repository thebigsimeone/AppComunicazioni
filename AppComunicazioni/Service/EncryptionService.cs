using AppComunicazioni.Interface;
using System.Security.Cryptography;
using System.Text;

namespace AppComunicazioni.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly string _key;

        public EncryptionService(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length != 32) // AES richiede 32 caratteri
                throw new ArgumentException("Chiave di crittografia non valida. Deve essere lunga 32 caratteri.");

            _key = key;
        }

        public string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_key);
            aes.GenerateIV();
            var iv = aes.IV;

            using var encryptor = aes.CreateEncryptor(aes.Key, iv);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            var encryptedBytes = ms.ToArray();
            var result = Convert.ToBase64String(iv) + ":" + Convert.ToBase64String(encryptedBytes);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(result));
        }

        public string Decrypt(string encryptedText)
        {
            var fullCipher = Encoding.UTF8.GetString(Convert.FromBase64String(encryptedText));
            var parts = fullCipher.Split(":");
            var iv = Convert.FromBase64String(parts[0]);
            var cipherText = Convert.FromBase64String(parts[1]);

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_key);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipherText);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
    }
}
