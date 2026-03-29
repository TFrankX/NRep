using Microsoft.Extensions.Configuration;

namespace WebServer.Services
{
    public static class SecretsConfigurationExtensions
    {
        /// <summary>
        /// Adds secrets configuration provider that replaces @variable patterns
        /// with decrypted values from ~/.config/store/sec_{variable}
        /// </summary>
        public static IConfigurationBuilder AddSecretsConfiguration(this IConfigurationBuilder builder)
        {
            // Build intermediate configuration to read existing values
            var intermediateConfig = builder.Build();

            // Add secrets provider that will override @variables
            builder.Add(new SecretsConfigurationSource(intermediateConfig));

            return builder;
        }
    }
}
