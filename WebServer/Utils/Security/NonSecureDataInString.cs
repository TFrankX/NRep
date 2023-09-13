using System;
using System.Text;

namespace WebServer.Utils.Security
{
    /// <summary>
    /// Простейшая имплементация ISecureData без поддержки реальной защиты данных
    /// </summary>
    public class NonSecureDataInString : ISecureData
    {
        private readonly string stringData;

        /// <summary>
        /// Создание экземпляра
        /// </summary>
        /// <param name="bytes"></param>
        public NonSecureDataInString(byte[] bytes)
        {
            stringData = Encoding.UTF8.GetString(bytes);
        }

        /// <inheritdoc />
        public void AppendChar(char c)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Clear()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public string GetString(TimeSpan? isValidFor = null)
        {
            return stringData;
        }

        /// <inheritdoc />
        public void InsertAt(int index, char c)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void SetAt(int index, char c)
        {
            throw new NotImplementedException();
        }
    }
}
