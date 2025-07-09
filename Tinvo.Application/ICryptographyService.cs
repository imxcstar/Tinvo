using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions;

namespace Tinvo.Application
{
    /// <summary>
    /// Provides encryption services using an AES key derived from a machine fingerprint.
    /// </summary>
    public class MachineFingerprintCryptographyService : ICryptographyService
    {
        // The AES key is generated once from a machine fingerprint and stored statically.
        private static readonly byte[] s_key = CreateKeyFromFingerprint();

        /// <summary>
        /// Creates a 16-byte (128-bit) encryption key from a machine fingerprint using MD5.
        /// </summary>
        private static byte[] CreateKeyFromFingerprint()
        {
            string fingerprint = GetMachineFingerprint();
            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(fingerprint);
                return md5.ComputeHash(inputBytes); // MD5 produces a 16-byte hash.
            }
        }

        /// <summary>
        /// Generates a reasonably unique identifier string for the current machine.
        /// </summary>
        private static string GetMachineFingerprint()
        {
            var fingerprint = new StringBuilder();
            fingerprint.Append(Environment.ProcessorCount);
            fingerprint.Append(Environment.MachineName);
            fingerprint.Append(Environment.OSVersion.VersionString);

            try
            {
                var macAddress = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .Select(nic => nic.GetPhysicalAddress().ToString())
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(macAddress))
                {
                    fingerprint.Append(macAddress);
                }
            }
            catch { /* Ignore errors if network interfaces can't be read. */ }

            return fingerprint.ToString();
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            using (var aes = Aes.Create())
            {
                aes.Key = s_key;
                aes.GenerateIV();
                var iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
                using (var memoryStream = new MemoryStream())
                {
                    memoryStream.Write(iv, 0, iv.Length);
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        var plainTextBytes = Encoding.Unicode.GetBytes(plainText);
                        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                        cryptoStream.FlushFinalBlock();
                    }

                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }

        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            var encryptedBytes = Convert.FromBase64String(encryptedText);

            using (var aes = Aes.Create())
            {
                aes.Key = s_key;

                var iv = new byte[16]; // AES IV is always 16 bytes
                Array.Copy(encryptedBytes, 0, iv, 0, iv.Length);

                using (var decryptor = aes.CreateDecryptor(aes.Key, iv))
                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(encryptedBytes, iv.Length, encryptedBytes.Length - iv.Length);
                        cryptoStream.FlushFinalBlock();
                    }

                    return Encoding.Unicode.GetString(memoryStream.ToArray());
                }
            }
        }
    }
}
