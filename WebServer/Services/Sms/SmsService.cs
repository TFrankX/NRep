using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace WebServer.Services.Sms
{
    public class SmsService : ISmsService
    {
        private readonly ILogger<SmsService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _smsApiSecret;

        public SmsService(ILogger<SmsService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _smsApiSecret = _configuration["SmsApi:Secret"] ??
                throw new InvalidOperationException("SMS API secret not configured. Add SmsApi:Secret to configuration.");
        }

        public string GenerateCode(int length = 5)
        {
            if (length < 4 || length > 8)
                length = 5;

            int min = (int)Math.Pow(10, length - 1);
            int max = (int)Math.Pow(10, length) - 1;

            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            int randomValue = Math.Abs(BitConverter.ToInt32(bytes, 0));

            return (min + (randomValue % (max - min + 1))).ToString();
        }

        public async Task<bool> SendCodeAsync(string phoneNumber, string sender, string code)
        {
            try
            {
                var normalizedPhone = NormalizePhoneNumber(phoneNumber);
                if (string.IsNullOrEmpty(normalizedPhone))
                {
                    _logger.LogWarning("Invalid phone number format: {PhoneNumber}", phoneNumber);
                    return false;
                }

                var messageBase64 = Base64Encode(code);
                var signatureData = $"{messageBase64}{normalizedPhone}{sender}";
                var signature = ComputeHmacSha256(_smsApiSecret, signatureData);

                var requestBody = $"{{\"messageText\":\"{messageBase64}\",\"msisdn\":\"{normalizedPhone}\",\"partnerId\":\"{sender}\"}}";

                _logger.LogInformation("SMS API Request: Phone={Phone}, Sender={Sender}, SignatureData={SigData}",
                    MaskPhoneNumber(normalizedPhone), sender, signatureData);

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Add("Signature", signature);

                var response = await client.PostAsync("https://api3.mobilipay.com:8209/omn/api/v2/init", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("SMS API Response: StatusCode={StatusCode}, Body={Body}",
                    (int)response.StatusCode, responseBody);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("SMS sent successfully to {PhoneNumber}", MaskPhoneNumber(normalizedPhone));
                    return true;
                }

                _logger.LogWarning("SMS send failed: StatusCode={StatusCode}, Response={Response}, Phone={PhoneNumber}",
                    (int)response.StatusCode, responseBody, MaskPhoneNumber(normalizedPhone));
                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error sending SMS to {PhoneNumber}. Check network/firewall.", MaskPhoneNumber(phoneNumber));
                return false;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout sending SMS to {PhoneNumber}. API not responding.", MaskPhoneNumber(phoneNumber));
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", MaskPhoneNumber(phoneNumber));
                return false;
            }
        }

        public bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            var normalized = NormalizePhoneNumber(phoneNumber);
            return !string.IsNullOrEmpty(normalized) && normalized.Length >= 10 && normalized.Length <= 15;
        }

        private string NormalizePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            var num = Regex.Replace(phoneNumber.Trim(), @"[\s()+_-]", "");

            if (num.Length == 8)
            {
                num = $"357{num}";
            }

            return num;
        }

        private static string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 6)
                return "***";

            return phoneNumber.Substring(0, 3) + "****" + phoneNumber.Substring(phoneNumber.Length - 3);
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        private static string ComputeHmacSha256(string secret, string message)
        {
            var keyBytes = Encoding.ASCII.GetBytes(secret);
            var messageBytes = Encoding.ASCII.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
