namespace EsquireVRN.Utils
{
    using System.Security.Cryptography;

    public static class EncryptionService
    {
        // Method to encrypt a plaintext string using AES encryption with a client-specific key and IV.
        public static string EncryptString(string plainText)
        {


            byte[] _key = Convert.FromBase64String("RXNxdWlyZTEjQFNlY3JldA==");
            // Convert the base64-encoded IV to a byte array.
            byte[] _iv = Convert.FromBase64String("M3g2TkQlT2I1RzU1UGtYJQ==");

            // Create a new instance of the AES algorithm.
            using (var aesAlg = Aes.Create())
            {
                // Set the key for the AES algorithm.
                aesAlg.Key = _key;
                // Set the IV for the AES algorithm.
                aesAlg.IV = _iv;
                // Create an encryptor from the AES instance to encrypt data.
                var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                // MemoryStream is used to hold the encrypted bytes.
                using (var msEncrypt = new MemoryStream())
                {
                    // CryptoStream for cryptographic transformation of data.
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    // StreamWriter to write the plaintext to the stream in a particular encoding.
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        // Write the plaintext to the crypto stream to perform encryption.
                        swEncrypt.Write(plainText);
                    }
                    // Convert the encrypted bytes from the memory stream to a base64 string.
                    return Convert.ToBase64String(msEncrypt.ToArray()).Replace("=", "-").Replace("+", "_").Replace("/", ".");
                }
            }
        }

        // Method to decrypt a ciphertext string using AES decryption with a client-specific key and IV.
        public static string Decrypt(string cipherText, string salt, string iv)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] ivBytes = Convert.FromBase64String(iv);

            using var aes = Aes.Create();
            var key = new Rfc2898DeriveBytes(
                "f2d60b8e54b327717652e0603340b25ec44410f13c3ada5c1f11bce9f92fe2ef",
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

        public static bool ValidatePassword(string enteredPassword, string encryptedPassword, string salt, string iv)
        {
            var decryptedPassword = Decrypt(encryptedPassword, salt, iv);
            return decryptedPassword == enteredPassword;
        }

        private static string EncryptPassword(string plainText, string salt, string iv)
        {
            byte[] key = Convert.FromBase64String(salt);
            byte[] ivBytes = Convert.FromBase64String(iv);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = ivBytes;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray())
                        .Replace("=", "-")
                        .Replace("+", "_")
                        .Replace("/", ".");
                }
            }
        }

    }
}
