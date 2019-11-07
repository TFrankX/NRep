using System;

namespace QuintetLab.ExchangeEngine.Contracts.CryptoSpot.V1
{
    /// <summary>
    /// Flags to define crypto-asset status
    /// </summary>
    [Flags]
    public enum CryptoAssetStatus: uint
    {
        /// <summary>
        /// Deposit of this asset is allowed
        /// </summary>
        DepositAllowed = 0x00_01,

        /// <summary>
        /// Withdraw of this asset is allowed
        /// </summary>
        WithdrawAllowed = 0x00_02,

        /// <summary>
        /// Trading on the instruments, witch include this asset, is allowed
        /// </summary>
        TradingAllowed = 0x00_04,

        /// <summary>
        /// All operations with this asset are allowed
        /// </summary>
        Active = DepositAllowed | WithdrawAllowed | TradingAllowed,

        /// <summary>
        /// This asset is delisted. If this flag is set all others XxxAllowed flags will be ignored
        /// </summary>
        Delisted = 0x80_00,

    }
}
