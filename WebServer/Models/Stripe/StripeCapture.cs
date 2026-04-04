using System;
namespace WebServer.Models.Stripe
{
	public class StripeCapture
	{

            //public string? ProductName { get; set; }
            //public string? ProductDescription { get; set; }
            public float Amount { get; set; }
            //public string? Currency { get; set; }
            public string? SessionId { get; set; }
            public ulong? StationId { get; set; }
            public string? StationName { get; set; }
            public ulong? PowerBankId { get; set; }
    }
}

