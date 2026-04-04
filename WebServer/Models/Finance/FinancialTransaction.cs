using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebServer.Models.Finance
{
    public enum TransactionType
    {
        Capture = 1,    // Payment captured (money taken from customer)
        Refund = 2,     // Refund issued
        Hold = 3,       // Payment hold (authorization)
        Release = 4     // Hold released without capture
    }

    public class FinancialTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// Transaction timestamp
        /// </summary>
        public DateTime TransactionTime { get; set; }

        /// <summary>
        /// Type of transaction
        /// </summary>
        public TransactionType Type { get; set; }

        /// <summary>
        /// Amount in EUR (positive for captures, positive for refunds - sign determined by Type)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Station ID where transaction occurred
        /// </summary>
        public ulong StationId { get; set; }

        /// <summary>
        /// Station name (denormalized for faster queries)
        /// </summary>
        public string StationName { get; set; } = "";

        /// <summary>
        /// PowerBank ID involved in transaction
        /// </summary>
        public ulong PowerBankId { get; set; }

        /// <summary>
        /// Customer/User ID
        /// </summary>
        public string UserId { get; set; } = "";

        /// <summary>
        /// Customer name/email for display
        /// </summary>
        public string CustomerName { get; set; } = "";

        /// <summary>
        /// Stripe PaymentIntent ID or other payment reference
        /// </summary>
        public string PaymentReference { get; set; } = "";

        /// <summary>
        /// Session ID for linking related transactions
        /// </summary>
        public string SessionId { get; set; } = "";

        /// <summary>
        /// Card info (e.g. "Visa *4242")
        /// </summary>
        public string CardInfo { get; set; } = "";

        /// <summary>
        /// Card expiry (e.g. "03/2028")
        /// </summary>
        public string CardExpiry { get; set; } = "";

        /// <summary>
        /// Card country code (e.g. "CY")
        /// </summary>
        public string CardCountry { get; set; } = "";

        /// <summary>
        /// Additional info/description
        /// </summary>
        public string Description { get; set; } = "";

        // Helper properties
        [NotMapped]
        public string StationId_Str => StationId.ToString();

        [NotMapped]
        public string PowerBankId_Str => PowerBankId.ToString();

        [NotMapped]
        public string TypeName => Type.ToString();

        [NotMapped]
        public decimal SignedAmount => Type == TransactionType.Refund ? -Amount : Amount;
    }
}
