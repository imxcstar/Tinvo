using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tinvo.Abstractions
{
    /// <summary>
    /// Defines a service for encrypting and decrypting strings.
    /// </summary>
    public interface ICryptographyService
    {
        /// <summary>
        /// Encrypts a plain text string.
        /// </summary>
        /// <param name="plainText">The text to encrypt.</param>
        /// <returns>The Base64 representation of the encrypted data.</returns>
        string Encrypt(string plainText);

        /// <summary>
        /// Decrypts an encrypted string.
        /// </summary>
        /// <param name="encryptedText">The Base64 encrypted text to decrypt.</param>
        /// <returns>The original plain text.</returns>
        /// <exception cref="System.Security.Cryptography.CryptographicException">Thrown if decryption fails.</exception>
        string Decrypt(string encryptedText);
    }
}
