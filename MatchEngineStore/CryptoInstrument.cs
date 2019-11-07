using System;
using QuintetLab.ExchangeEngine.Contracts.Common.V1;

namespace QuintetLab.ExchangeEngine.Contracts.CryptoSpot.V1
{
    /// <summary>
    ///  An instrument used in crypto-spot trading
    /// </summary>
    public class CryptoInstrument : CustomSettingsContainer
    {
        /// <summary>
        /// Instrument ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Short unique name of the instrument
        /// </summary>
        public string Ticker { get; set; }

        /// <summary>
        /// Base asset id of the instrument
        /// </summary>
        public long BaseAssetId { get; set; }

        /// <summary>
        /// Quote asset id of the instrument
        /// </summary>
        public long QuoteAssetId { get; set; }

        /// <summary>
        /// Id of the asset in which the fee is taken by default
        /// </summary>
        public long DefaultFeeAssetId { get; set; }

        /// <summary>
        /// Status of the crypto-instrument
        /// </summary>
        public CryptoInstrumentStatus Status { get; set; }

        /// <summary>
        /// The minimum price movement of the instrument (the smallest price increment of an order placed for the instrument)
        /// </summary>
        public decimal TickSize { get; set; }

        /// <summary>
        /// Minimum price of an order placed for the instrument
        /// </summary>
        public decimal MinPrice { get; set; }

        /// <summary>
        /// Maximum price of an order placed for the instrument
        /// </summary>
        public decimal MaxPrice { get; set; }

        /// <summary>
        /// Minimum volume (in base asset) of an order
        /// </summary>
        public decimal MinVolumeBase { get; set; }

        /// <summary>
        /// Maximum volume (in base asset) of an order
        /// </summary>
        public decimal MaxVolumeBase { get; set; }

        /// <summary>
        /// Minimum volume in quote asset (order volume * price) of an order
        /// </summary>
        public decimal MinVolumeQuote { get; set; }

        /// <summary>
        /// Maximum volume in quote asset (order volume * price) of an order
        /// </summary>
        public decimal MaxVolumeQuote { get; set; }

        /// <summary>
        /// The step of order volume
        /// </summary>
        public decimal BaseVolumeStep { get; set; }

        /// <summary>
        /// Instrument creation timestamp
        /// </summary>
        public DateTimeOffset CreationTimestamp { get; set; }
    }
}
