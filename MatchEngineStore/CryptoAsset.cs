using QuintetLab.ExchangeEngine.Contracts.Common.V1;

namespace QuintetLab.ExchangeEngine.Contracts.CryptoSpot.V1
{
    /// <summary>
    /// An asset used in crypto-spot trading. It can be crypto-currency, token, fiat currency, etc.
    /// </summary>
    public class CryptoAsset: CustomSettingsContainer
    {
        /// <summary>
        /// Asset ID
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Short unique name of the asset
        /// </summary>
        public string Ticker { get; set; }

        /// <summary>
        /// Name of the asset
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Crypto-asset type
        /// </summary>
        public CryptoAssetType AssetType { get; set; }

        /// <summary>
        /// Status of the crypto-asset
        /// </summary>
        public CryptoAssetStatus Status { get; set; }

        /// <summary>
        /// Minimum possible amount of the asset. Smaller portions of the asset are rounded with this step.
        /// </summary>
        public decimal AmountStep { get; set; }
    }
}
