using System;

namespace QuintetLab.ExchangeEngine.Contracts.CryptoSpot.V1
{
    /// <summary>
    /// Flags to define crypto-instrument status
    /// </summary>
    [Flags]
    public enum CryptoInstrumentStatus : uint
    {
       
        /// <summary>
        /// Trading on the instruments is allowed
        /// </summary>
        TradingAllowed = 0x00_04,

        /// <summary>
        /// All operations with this instrument are allowed
        /// </summary>
        Active = TradingAllowed,

        /// <summary>
        /// This instrument is delisted. If this flag is set all others XxxAllowed flags will be ignored
        /// </summary>
        Delisted = 0x80_00,
    }
}
