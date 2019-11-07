using System;
using System.Text.Json;

namespace QuintetLab.ExchangeEngine.Contracts.Common.V1
{
    /// <summary>
    /// Class for storing custom settings in JSON format
    /// </summary>
    public class CustomSettingsContainer
    {
        /// <summary>
        /// Settings presented by the JSON-serialized object
        /// </summary>
        public JsonElement? Settings { get; set; }

        /// <summary>
        /// Get settings as specified class
        /// </summary>
        /// <typeparam name="T"><see cref="Type"/> of class that will be returned</typeparam>
        /// <returns>Instance of class <typeparamref name="T"/> or <see langword="null"/></returns>
        public T GetSettings<T>() where T : class
        {
            return Settings == null ? null : JsonSerializer.Deserialize<T>(Settings.Value.GetRawText());
        }

        /// <summary>
        /// Set settings with specified object of class <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"><see cref="Type"/> of settings object</typeparam>
        /// <param name="settings">Object of class <typeparamref name="T"/> which contains settings</param>
        public void SetSettings<T>(T settings) where T : class
        {
            if (settings == null)
            {
                Settings = null;
                return;
            }
            Settings = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(settings));
        }
    }
}
