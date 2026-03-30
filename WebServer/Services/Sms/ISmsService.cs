namespace WebServer.Services.Sms
{
    public interface ISmsService
    {
        /// <summary>
        /// Generate a random verification code
        /// </summary>
        string GenerateCode(int length = 5);

        /// <summary>
        /// Send SMS with verification code
        /// </summary>
        Task<bool> SendCodeAsync(string phoneNumber, string sender, string code);

        /// <summary>
        /// Validate phone number format
        /// </summary>
        bool IsValidPhoneNumber(string phoneNumber);
    }
}
