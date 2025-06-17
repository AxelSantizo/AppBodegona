using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AppBodegona.Services
{
    public static class EncryptionService
    {
        private static readonly string rawKey = "SecureQRKey12345"; // 🔐 16 caracteres exactos
        private static readonly byte[] key = Encoding.UTF8.GetBytes(rawKey);
        private static readonly byte[] iv = new byte[16]; // ⚠️ IV fijo en ceros (para demo)

        public static string Encrypt(string plainText)
        {
            byte[] encrypted;

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }

                    encrypted = ms.ToArray(); // ✅ Ahora está dentro del scope de ms
                }
            }

            return Convert.ToBase64String(encrypted);
        }

        public static string Decrypt(string cipherTextBase64)
        {
            byte[] cipherText = Convert.FromBase64String(cipherTextBase64);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }
    }
}
