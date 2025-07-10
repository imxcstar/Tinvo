using System.Security.Cryptography;
using System.Text;
using Tinvo.Abstractions;

namespace Tinvo.Services
{
    public class BasicCryptographyService : ICryptographyService
    {
        private const int XorKey = 233;

        /// <summary>
        /// 使用简单的异或算法加密纯文本字符串。
        /// </summary>
        /// <param name="plainText">要加密的文本。</param>
        /// <returns>加密后数据的 Base64 表示形式。</returns>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            byte[] data = Encoding.UTF8.GetBytes(plainText);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(data[i] ^ XorKey);
            }

            return Convert.ToBase64String(data);
        }

        /// <summary>
        /// 解密一个经过简单异或加密的字符串。
        /// </summary>
        /// <param name="encryptedText">要解密的 Base64 加密文本。</param>
        /// <returns>原始的纯文本。</returns>
        /// <exception cref="System.Security.Cryptography.CryptographicException">如果解密失败（例如，无效的Base64字符串），则抛出异常。</exception>
        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                return string.Empty;
            }

            try
            {
                byte[] data = Convert.FromBase64String(encryptedText);
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (byte)(data[i] ^ XorKey);
                }

                return Encoding.UTF8.GetString(data);
            }
            catch (FormatException ex)
            {
                // 如果输入的字符串不是有效的Base64格式，则抛出加密异常。
                throw new CryptographicException("The input is not a valid Base64 string.", ex);
            }
        }
    }
}
