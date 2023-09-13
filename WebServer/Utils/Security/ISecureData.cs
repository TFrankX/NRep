using System;
using System.Security.Cryptography;

namespace WebServer.Utils.Security
{
    /// <summary>
    /// Делегат - фабрика секьюрных данных
    /// </summary>
    /// <param name="secret"></param>
    /// <returns></returns>
    public delegate ISecureData SecureDataFactory(byte[] secret);

    /// <summary>
    /// Контейнер секретных данных
    /// </summary>
    public interface ISecureData
    {
        /// <summary>
        /// Get string
        /// </summary>
        /// <param name="isValidFor">If parameter is not set, the default value 10 seconds will be used</param>
        /// <returns>Return string</returns>
        string GetString(TimeSpan? isValidFor = null);

        /// <summary>
        /// Appends a character to the end of the current secure string
        /// </summary>
        /// <param name="c">A character to append to this secure string</param>
        /// <exception cref="ObjectDisposedException">This secure string has already been disposed</exception>
        /// <exception cref="InvalidOperationException">This secure string is read-only</exception>
        /// <exception cref="ArgumentOutOfRangeException">Performing this operation would make the length of this secure string greater than 65,536 characters</exception>
        /// <exception cref="CryptographicException">An error occurred while protecting or unprotecting the value of this secure string</exception>
        void AppendChar(char c);

        /// <summary>
        /// Deletes the value of the current secure string
        /// </summary>
        /// <exception cref="ObjectDisposedException">This secure string has already been disposed</exception>
        /// <exception cref="InvalidOperationException">This secure string is read-only</exception>
        void Clear();

        /// <summary>
        /// Inserts a character in this secure string at the specified index position
        /// </summary>
        /// <param name="index">The index position where parameter c is inserted</param>
        /// <param name="c">The character to insert</param>
        /// <exception cref="ObjectDisposedException">This secure string has already been disposed</exception>
        /// <exception cref="InvalidOperationException">This secure string is read-only</exception>
        /// <exception cref="ArgumentOutOfRangeException">index is less than zero, or greater than the length of this secure string. -or-
        /// Performing this operation would make the length of this secure string greater than 65,536 characters
        /// </exception>
        /// <exception cref="CryptographicException">An error occurred while protecting or unprotecting the value of this secure string</exception>
        void InsertAt(int index, char c);

        /// <summary>
        /// Removes the character at the specified index position from this secure string
        /// </summary>
        /// <param name="index">The index position of a character in this secure string</param>
        /// <exception cref="ObjectDisposedException">This secure string has already been disposed</exception>
        /// <exception cref="InvalidOperationException"> This secure string is read-only</exception>
        /// <exception cref="ArgumentOutOfRangeException">index is less than zero, or greater than or equal to the length of this secure string</exception>
        void RemoveAt(int index);

        /// <summary>
        /// Replaces the existing character at the specified index position with another
        /// </summary>
        /// <param name="index">The index position of an existing character in this secure string</param>
        /// <param name="c">A character that replaces the existing character</param>
        /// <exception cref="ObjectDisposedException">This secure string has already been disposed</exception>
        /// <exception cref="InvalidOperationException">This secure string is read-only</exception>
        /// <exception cref="ArgumentOutOfRangeException">index is less than zero, or greater than or equal to the length of this secure string</exception>
        /// <exception cref="CryptographicException">An error occurred while protecting or unprotecting the value of this secure string</exception>
        void SetAt(int index, char c);

    }
}
