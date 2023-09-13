using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace WebServer.Utils.Security
{
    /// <summary>
    /// Поставщик паролей к базам данных 
    /// </summary>
    public interface IDbPasswordProvider
    {
        /// <summary>
        /// По имени базы данных предоставляет пароль
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="database"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        string ProvidePasswordCallback(string host, int port, string database, string username);
    }

    /// <inheritdoc />
    public class DbPasswordProvider : IDbPasswordProvider
    {
        readonly SecureDataFactory secureDataFactory;
        readonly Dictionary<string, ISecureData> passwords;

        /// <summary>
        /// Конструктор, что еще тут скажешь
        /// </summary>
        /// <param name="secureDataFactory"></param>
        /// <param name="passwords">Соответствие: имя базы данных -> пароль</param>
        public DbPasswordProvider(SecureDataFactory secureDataFactory, Dictionary<string, string> passwords)
        {
            this.secureDataFactory = secureDataFactory;
            this.passwords = passwords.ToDictionary(k => k.Key, k => secureDataFactory(Encoding.UTF8.GetBytes(k.Value)));
        }

        /// <inheritdoc />
        public string ProvidePasswordCallback(string host, int port, string database, string username)
        {
            if (passwords.TryGetValue(database, out ISecureData secureData) == false)
                throw new ArgumentException($"Не задан пароль для базы данных {database}", nameof(database));
            return secureData.GetString();
        }
    }
}
