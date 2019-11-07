using System;
using QuintetLab.ExchangeEngine.Contracts.Common.V1;

namespace QuintetLab.ExchangeEngine.Contracts.CryptoSpot.V1
{
    /// <summary>
    /// An asset used in crypto-spot trading. It can be crypto-currency, token, fiat currency, etc.
    /// </summary>
    public class CryptoOrder : CustomSettingsContainer
    {
        /// <summary>
        /// Trade instrument id for which the order should be placed
        /// </summary>
        public int InstrumentId { get; set; }

        /// <summary>
        /// Order creation timestamp
        /// </summary>
        public DateTimeOffset CreationTimestamp { get; set; }

        /// <summary>
        /// Last update order timestamp
        /// </summary>
        public DateTimeOffset LastUpdateTimestamp { get; set; }

        /// <summary>
        /// Order type
        /// </summary>
        public CryptoOrderType OrderType { get; set; }

        /// <summary>
        /// Order price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Stop price
        /// </summary>
        public decimal StopPrice { get; set; }

        /// <summary>
        /// The amount of base currency to be bought or sold
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// Remaining volume of the order (volume that hasn’t been filled)
        /// </summary>
        public decimal RemainingVolume { get; set; }

        /// <summary>
        /// Length of time over which the order will continue working before it’s cancelled
        /// </summary>
        public CryptoOrderTimeInForce TimeInForce { get; set; }

        /// <summary>
        /// Order status
        /// </summary>
        public CryptoOrderStatus Status { get; set; }

        /// <summary>
        /// ID of an order that served as a base order for the current one
        /// </summary>
        public int ParentOrderId { get; set; }

        /// <summary>
        /// ID of an order that has been created based on the current order
        /// </summary>
        public int ChildOrderId { get; set; }
    }
}
