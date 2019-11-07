namespace QuintetLab.ExchangeEngine.Contracts.CryptoSpot.V1
{
    /// <summary>
    /// Types of assets used in crypto-spot trading
    /// </summary>
    public enum CryptoAssetType
    {
        /// <summary>
        /// Not defined type
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A crypto coin with its own blockchain
        /// </summary>
        Coin = 1,

        /// <summary>
        /// Crypto token
        /// </summary>
        Token = 2,

        /// <summary>
        /// Fiat currency
        /// </summary>
        Fiat = 10
    }
}
