using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TrustInnova
{
    public static class StringExtend
    {
        public static string UTF8PtrToStr(this IntPtr utf8)
        {
            return Marshal.PtrToStringUTF8(utf8);
        }

        public static IntPtr StrToUtf8Ptr(this string str)
        {
            IntPtr ptr = IntPtr.Zero;

            if (str != null)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                ptr = Marshal.AllocHGlobal(bytes.Length + 1);
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
                Marshal.WriteByte(ptr, bytes.Length, 0);
            }

            return ptr;
        }
    }
}
