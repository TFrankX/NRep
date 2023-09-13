using System;
using System.Runtime.InteropServices;
using System.Security;

namespace WebServer.Utils.Security
{
    /// <summary>
    /// Class converting "SecureString" type to "String"
    /// </summary>
    public static class SecureStringHelper
    {
        /// <summary>
        /// Converting "SecureString" type to "String"
        /// </summary>
        /// <param name="value">The string that needs to be converted</param>
        /// <returns>String type "string" </returns>
        public static string ExtractString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }
}
