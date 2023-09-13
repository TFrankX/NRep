using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;


namespace WebServer.Utils.Security
{
    /// <summary>
    /// ISecureData implementation based on Microsoft SecureString class
    /// </summary>
    public sealed class SecureDataInSecureString : ISecureData, IDisposable
    {
        private readonly SecureString secureString = new SecureString();
        private readonly List<DataReturnedString> dataReturnedStrings = new List<DataReturnedString>();
        private bool clearingThreadIsRunning = false;

        /// <inheritdoc />
        public void Dispose()
        {
            secureString.Dispose();
        }

        /// <inheritdoc />
        public string GetString(TimeSpan? isValidFor = null)
        {
            var extractedString = SecureStringHelper.ExtractString(secureString);

            var dataReturnedString = new DataReturnedString()
            {
                LengthReturnedString = extractedString.Length,
                ReturnedString = extractedString,
                ValidTill = DateTimeOffset.UtcNow.Add(isValidFor ?? TimeSpan.FromSeconds(10))
            };

            lock (dataReturnedString)
            {
                dataReturnedStrings.Add(dataReturnedString);
                if (!clearingThreadIsRunning)
                {
                    new Thread(ClearingThreadFunc).Start();
                    clearingThreadIsRunning = true;
                }

            }

            return extractedString;
        }

        /// <inheritdoc />
        public void AppendChar(char c)
        {
            secureString.AppendChar(c);
        }

        /// <inheritdoc />
        public void Clear()
        {
            secureString.Clear();
        }

        /// <inheritdoc />
        public void InsertAt(int index, char c)
        {
            secureString.InsertAt(index, c);
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            secureString.RemoveAt(index);
        }

        /// <inheritdoc />
        public void SetAt(int index, char c)
        {
            secureString.SetAt(index, c);
        }

        class DataReturnedString
        {
            public string ReturnedString { get; set; }

            public int LengthReturnedString { get; set; }

            public DateTimeOffset ValidTill { get; set; }
        }


        private void ClearingThreadFunc()
        {
            while (true)
            {
                try
                {
                    lock (dataReturnedStrings)
                    {
                        foreach (var drs in dataReturnedStrings)
                        {
                            if (DateTimeOffset.UtcNow >= drs.ValidTill)
                            {
                                dataReturnedStrings.Remove(drs);
                                //TODO: rewrite the string value in memory
                            }
                        }

                        if (!dataReturnedStrings.Any())
                        {
                            clearingThreadIsRunning = false;
                            return;
                        }
                    }
                }
                catch
                {
                    // ignore
                }

                Thread.Sleep(10);
            }


        }
    }
}
