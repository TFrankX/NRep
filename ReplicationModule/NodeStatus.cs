namespace ReplicationModule
{
    public enum NodeStatus
    {
        /// <summary>
        /// Unknown status
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Node is ok
        /// </summary>
        Ok = 1,

        /// <summary>
        /// Pre-fail condition
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Node is fail
        /// </summary>
        Fail = 3,
    }
}