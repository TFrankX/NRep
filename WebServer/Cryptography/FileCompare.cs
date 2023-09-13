using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace WebServer.Cryptography
{
    public static class FileCompare
    {
        /// <summary>
        /// Compute SHA256 hash value
        /// </summary>
        /// <param name="inputData">Input string value</param>
        /// <returns>SHA256 Hash string</returns>
        public static string ComputeHash(string inputData)
        {
            if (string.IsNullOrEmpty(inputData))
                return null;

            byte[] tmpSource = Encoding.ASCII.GetBytes(inputData);
            using SHA256 mySHA256 = SHA256.Create();
            byte[] tmpHash = mySHA256.ComputeHash(tmpSource);

            return ByteArrayToString(tmpHash);
        }

        /// <summary>
        /// Compute SHA256 hash for inputData and compare result with targetHash parameter
        /// </summary>
        /// <param name="inputData">Input string value</param>
        /// <param name="targetHash">SHA256 hash value</param>
        /// <returns>Compare result</returns>
        public static bool HashIsCorrect(string inputData, string targetHash)
        {
            return ComputeHash(inputData) == targetHash;
        }

        /// <summary>
        /// Convert byte array to string
        /// </summary>
        /// <param name="byteArray">Byte array</param>
        /// <returns>String value</returns>
        public static string ByteArrayToString(byte[] byteArray)
        {
            int i;
            StringBuilder sOutput = new StringBuilder(byteArray.Length);
            for (i = 0; i < byteArray.Length; i++)
            {
                sOutput.Append(byteArray[i].ToString("X2"));
            }
            return sOutput.ToString();
        }
    }
}
