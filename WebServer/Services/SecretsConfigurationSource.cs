using Microsoft.Extensions.Configuration;

namespace WebServer.Services
{
    public class SecretsConfigurationSource : IConfigurationSource
    {
        private readonly IConfigurationRoot _configuration;

        public SecretsConfigurationSource(IConfigurationRoot configuration)
        {
            _configuration = configuration;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new SecretsConfigurationProvider(_configuration);
        }
    }
}
