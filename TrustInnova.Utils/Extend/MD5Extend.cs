using System.Security.Cryptography;
using System.Text;

namespace TrustInnova.Utils.Extend
{
    public static class MD5Extend
    {
        public static byte[] ToMD5_Hash(this string input)
        {
            MD5 md5 = MD5.Create();
            byte[] byteOld = Encoding.UTF8.GetBytes(input);
            byte[] byteNew = md5.ComputeHash(byteOld);
            return byteNew;
        }

        public static string ToMD5_32(this byte[] input)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in input)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        public static string ToMD5_32(this string input)
        {
            return input.ToMD5_Hash().ToMD5_32();
        }

        public static string ToMD5_16(this byte[] input)
        {
            return input.ToMD5_32().Substring(8, 16);
        }

        public static string ToMD5_16(this string input)
        {
            return input.ToMD5_32().Substring(8, 16);
        }
    }
}
