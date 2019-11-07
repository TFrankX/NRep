using System.Collections.Generic;
using QuintetLab.ExchangeEngine.Contracts.CryptoSpot.V1;

namespace QuintetLab.MatchingEngine.Contracts.CryptoSpot.V1
{
    /// <summary>
    /// List of buy and sell orders organized by price level
    /// </summary>
    public class OrderBook
    {
        /// <summary>
        /// Order books
        /// </summary>
        public OrderBookData[] OrderBooks { get; set; }
        /// <summary>
        /// Order book data
        /// </summary>
        public class OrderBookData
        {
            /// <summary>
            /// Instrument used in crypto-spot trading
            /// </summary>
            public CryptoInstrument Instrument;

            /// <summary>
            /// Bid orders
            /// </summary>
            public CryptoOrder[] Bid;

            /// <summary>
            /// Ask orders
            /// </summary>
            public CryptoOrder[] Ask;

            /// <summary>
            /// Ask orders
            /// </summary>
            public Queue<string>  MessageQueue;

            /// <summary>
            /// Order book status
            /// </summary>
            public OrderBookStatus Status;
        }

        /// <summary>
        /// Order book status
        /// </summary>
        public enum OrderBookStatus
        {
            /// <summary>
            /// Not defined status
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// Initialization order book
            /// </summary>
            Initialization = 1,

            /// <summary>
            /// All operations with this order book are allowed
            /// </summary>
            Active = 2
        }
    }
}

