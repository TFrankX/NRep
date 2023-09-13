using System;
using System.Security.Cryptography;
using System.Text;
using WebServer.Cryptography;

namespace WebServer.Utils.Security
{
    /// <summary>
    /// 
    /// </summary>
    public static class CRandom
    {
        private static readonly RNGCryptoServiceProvider rnd = new RNGCryptoServiceProvider();

        /// <summary>
        /// Returns a random double value from the range [0; 1]
        /// </summary>
        /// <returns></returns>
        public static double GetDouble()
        {
            var data = new byte[8];
            rnd.GetBytes(data);
            return Convert.ToDouble(BitConverter.ToUInt64(data, 0)) / ulong.MaxValue;
        }

        /// <summary>
        /// Returns a random int value from the range [0; maxN]
        /// </summary>
        /// <param name="maxN"></param>
        /// <returns></returns>
        public static int GetInt(int maxN)
        {
            return (int)Math.Round(GetDouble() * maxN);
        }

        static readonly RandomGenerator random = new RandomGenerator();
        static readonly char[] alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToLowerInvariant().ToCharArray();
        /// <summary>
        /// Генерирует случайную строку из маленьких латинских букв
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string GetRandomString(int length)
        {
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(alpha[random.Next(0, alpha.Length)]);
            }
            return sb.ToString();
        }

    }
}