using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NeplusLicense.Crypto
{
    public static class CryptoService
    {
        public static string EncryptText(this string encryptString, string passPhrase)
        {
            var       clearBytes = Encoding.Unicode.GetBytes(encryptString);
            using var encryption = Aes.Create();
            var pdb = new Rfc2898DeriveBytes(passPhrase, new byte[]
            {
                0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76
            });
            if (encryption is null) return encryptString;
            encryption.Key = pdb.GetBytes(32);
            encryption.IV  = pdb.GetBytes(16);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryption.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(clearBytes, 0, clearBytes.Length);
            cs.Close();

            encryptString = Convert.ToBase64String(ms.ToArray());
            return encryptString;
        }

        public static string DecryptText(this string cipherText, string passPhrase)
        {
            var       cipherBytes = Convert.FromBase64String(cipherText);
            using var encryption  = Aes.Create();
            var pdb = new Rfc2898DeriveBytes(passPhrase, new byte[]
            {
                0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76
            });
            if (encryption is null) return cipherText;
            encryption.Key = pdb.GetBytes(32);
            encryption.IV  = pdb.GetBytes(16);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryption.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(cipherBytes, 0, cipherBytes.Length);
            cs.Close();

            cipherText = Encoding.Unicode.GetString(ms.ToArray());
            return cipherText;
        }

        public static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            foreach (var t in data)
            {
                sBuilder.Append(t.ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        // Verify a hash against a string.
        public static bool VerifyHash(HashAlgorithm hashAlgorithm, string input, string hash)
        {
            // Hash the input.
            var hashOfInput = GetHash(hashAlgorithm, input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            return comparer.Compare(hashOfInput, hash) == 0;
        }
    }
}