using System;
using QuintetLab.ExchangeEngine.Contracts.Common.V1;

namespace QuintetLab.ExchangeEngine.Contracts.CryptoSpot.V1
{
    /// <summary>
    /// An asset used in crypto-spot trading. It can be crypto-currency, token, fiat currency, etc.
    /// </summary>
    public class CryptoTrade: CustomSettingsContainer
    {
        /// <summary>
        /// Trade ID
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Unique order identifier
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Date and time when the trade took place
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Price for which the base currency was bought or sold
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Trade volume (the amount of base currency that was bought or sold)
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ulong GivenAssetId { get; set; }

        /// <summary>
        ///
        /// </summary>
        public decimal GivenAssetAmount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ulong ReceivedAssetId { get; set; }

        /// <summary>
        ///
        /// </summary>
        public decimal ReceivedAssetAmount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int FeeAssetId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public CryptoFeeType FeeType { get; set; }

        /// <summary>
        ///The amount of fee charged (negative value means rebate)
        /// </summary>
        public decimal FeeAmount { get; set; }
    }
}
