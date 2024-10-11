using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TrustInnova.Utils.Extend
{
    public static class StringExtend
    {
        /// <summary>
        /// 转换为密码MD5
        /// </summary>
        /// <param name="value"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        public static string ToUserPasswordMd5(this string value, string salt)
        {
            if (string.IsNullOrEmpty(value))
                throw new Exception("密码为空");
            return $"{value}:{salt}".ToMd5();
        }

        /// <summary>
        /// 转换为MD5
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToMd5(this string value)
        {
            using (var md5 = MD5.Create())
            {
                return string.Join("", md5.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(x => x.ToString("x2")));
            }
        }

        /// <summary>
        /// 转换为MD5(16位)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToMd5_16(this string value)
        {
            return value.ToMd5().Substring(8, 16);
        }

        /// <summary>
        /// 按最大值切割字符串，保证每条字符串长度都少于等于最大值
        /// </summary>
        /// <param name="input"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string[] SplitMax(this string input, int maxLength)
        {
            int numberOfStrings = (input.Length + maxLength - 1) / maxLength;
            string[] splitString = new string[numberOfStrings];

            for (int i = 0; i < numberOfStrings; i++)
            {
                int startIndex = i * maxLength;
                int length = Math.Min(maxLength, input.Length - startIndex);
                splitString[i] = input.Substring(startIndex, length);
            }

            return splitString;
        }

        /// <summary>
        ///     Remove the search string from the beginning of string if it exists
        /// </summary>
        /// <param name="text"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        public static string RemoveIfStartWith(this string text, string search)
        {
            var pos = text.IndexOf(search, StringComparison.Ordinal);
            return pos != 0 ? text : text.Substring(search.Length);
        }
    }
}
