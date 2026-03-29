using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace WebServer.Services
{
    public class SecretsConfigurationProvider : ConfigurationProvider
    {
        private readonly IConfigurationRoot _configuration;
        private static readonly Regex SecretPattern = new Regex(@"@(\w+)", RegexOptions.Compiled);

        public SecretsConfigurationProvider(IConfigurationRoot configuration)
        {
            _configuration = configuration;
        }

        public override void Load()
        {
            var replacedSecrets = new HashSet<string>();

            // Iterate through all configuration values and replace @variables
            foreach (var kvp in GetAllValues(_configuration))
            {
                var value = kvp.Value;
                if (string.IsNullOrEmpty(value)) continue;

                // Check if value contains @variable pattern
                var match = SecretPattern.Match(value);
                if (match.Success && value.StartsWith("@") || value.Contains("@"))
                {
                    var newValue = SecretPattern.Replace(value, m =>
                    {
                        var secretName = m.Groups[1].Value;
                        var secret = SecretProvider.GetSecret(secretName);

                        if (secret != null)
                        {
                            if (!replacedSecrets.Contains(secretName))
                            {
                                Console.WriteLine($"[SecretsConfig] Replaced secret: @{secretName}");
                                replacedSecrets.Add(secretName);
                            }
                            return secret;
                        }
                        else
                        {
                            Console.WriteLine($"[SecretsConfig] WARNING: Secret not found: @{secretName}");
                            return m.Value; // Keep original if secret not found
                        }
                    });

                    if (newValue != value)
                    {
                        Data[kvp.Key] = newValue;
                    }
                }
            }

            Console.WriteLine($"[SecretsConfig] Total secrets replaced: {replacedSecrets.Count}");
        }

        private static IEnumerable<KeyValuePair<string, string?>> GetAllValues(IConfiguration config, string parentKey = "")
        {
            foreach (var section in config.GetChildren())
            {
                var key = string.IsNullOrEmpty(parentKey) ? section.Key : $"{parentKey}:{section.Key}";

                if (section.Value != null)
                {
                    yield return new KeyValuePair<string, string?>(key, section.Value);
                }

                // Recurse into children
                foreach (var child in GetAllValues(section, key))
                {
                    yield return child;
                }
            }
        }
    }
}
