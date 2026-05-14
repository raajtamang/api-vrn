using System.Security.Cryptography;

namespace EsquireVRN.Utils
{
    public class PasswordEncryptionService
    {
        private readonly string _masterKey;

        public PasswordEncryptionService(string masterKey)
        {
            _masterKey = masterKey;
        }

        // Encrypt
        public (string cipherText, string salt, string iv) Encrypt(string plainText)
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(32);
            byte[] ivBytes = RandomNumberGenerator.GetBytes(16);

            using var aes = Aes.Create();

            var key = new Rfc2898DeriveBytes(
                _masterKey,
                saltBytes,
                100000,
                HashAlgorithmName.SHA256);

            aes.Key = key.GetBytes(32);
            aes.IV = ivBytes;

            using var encryptor = aes.CreateEncryptor();

            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using var sw = new StreamWriter(cs);

            sw.Write(plainText);
            sw.Close();

            return (
                Convert.ToBase64String(ms.ToArray()),
                Convert.ToBase64String(saltBytes),
                Convert.ToBase64String(ivBytes)
            );
        }

        // Decrypt
        public string Decrypt(string cipherText, string salt, string iv)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] ivBytes = Convert.FromBase64String(iv);

            using var aes = Aes.Create();

            var key = new Rfc2898DeriveBytes(
                _masterKey,
                saltBytes,
                100000,
                HashAlgorithmName.SHA256);

            aes.Key = key.GetBytes(32);
            aes.IV = ivBytes;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(cipherBytes);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }

        public bool ValidatePassword(string enteredPassword, string encryptedPassword, string salt, string iv)
        {
            var decryptedPassword = Decrypt(encryptedPassword,salt,iv);
            return decryptedPassword == enteredPassword;
        }
    }
}
