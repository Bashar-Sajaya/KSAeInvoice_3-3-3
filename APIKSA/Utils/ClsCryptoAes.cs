using System;
using System.Security.Cryptography;
using System.Text;

namespace SJYtoolsLibrary.Services
{
    public class ClsCryptoAes
    {
        private readonly Aes aes;

        public ClsCryptoAes(string strIV, int productGroup = 1)
        {
            // Create an instance of Aes using the recommended Create method
            aes = Aes.Create();

            // Ensure the IV is 16 bytes (128 bits)
            if (strIV.Length > 16)
            {
                //strIV = strIV[..16];
                strIV = strIV.Substring(0, 16);

            }
            else if (strIV.Length < 16 && strIV.Length != 0)
            {
                strIV = strIV.PadRight(16, '*');
            }
            else
            {
                switch (productGroup)
                {
                    case 1:
                        strIV = "SAJAYA@2008-2020";
                        break;
                    case 2:
                        strIV = "$@J@Y@_3RP$Y$T3M";
                        break;
                }
            }

            aes.IV = Encoding.UTF8.GetBytes(strIV);

            // Ensure the key is 32 bytes (256 bits) for AES-256 encryption
            string key = "";
            switch (productGroup)
            {
                case 1:
                    key = "SAJAYA@2008-2020SAJAYA@2008-2020";
                    break;
                case 2:
                    key = "$@J@Y@3NT3RPRI$3R3$OURC3PL@NNING";
                    break;
            }

            if (key.Length > 32)
            {
                //key = key[..32];
                key = key.Substring(0, 32);

            }
            else if (key.Length < 32)
            {
                key = key.PadRight(32, '*');
            }
            aes.Key = Encoding.UTF8.GetBytes(key);

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
        }

        public string Encrypt(string strPlainText)
        {
            byte[] originalBytes = Encoding.UTF8.GetBytes(strPlainText);
            ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] encryptedBytes = encryptor.TransformFinalBlock(originalBytes, 0, originalBytes.Length);
            return Convert.ToBase64String(encryptedBytes);
        }

        public string Decrypt(string encryptedText)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] originalBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            return Encoding.UTF8.GetString(originalBytes);
        }
    }
}
