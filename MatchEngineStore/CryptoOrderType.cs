namespace QuintetLab.ExchangeEngine.Contracts.CryptoSpot.V1
{ 
    /// <summary>
    /// Types of order used in crypto-spot trading
    /// </summary>
    public enum CryptoOrderType
    {
        /// <summary>
        /// Not defined type
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Order to buy or sell a limited amount of a specified currency at a specified price or better
        /// </summary>
        Limit = 1,

        /// <summary>
        /// Order to immediately buy or sell a limited amount of a specified currency at a current market price
        /// </summary>
        Market = 2,

        /// <summary>
        /// Order that waits for the market price to reach a certain threshold (called the stop price) and when it happens
        /// </summary>
        StopLimit = 3
    }
}
