namespace QuintetLab.ExchangeEngine.Contracts.CryptoSpot.V1
{
    /// <summary>
    /// The length of time over which the order will continue working before it’s cancelled
    /// </summary>
    public enum CryptoOrderTimeInForce
    {
        /// <summary>
        /// Not defined type
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Good-Til-Cancelled
        /// </summary>
        GTS = 1,

        /// <summary>
        /// Immediate-Or-Cancel 
        /// </summary>
        IOC = 2,

        /// <summary>
        /// Fill-Or-Kill (
        /// </summary>
        FOC = 3

    }
}
