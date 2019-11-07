namespace ReplicationModule
{
    public enum CommandType
    {
        /// <summary>
        /// Unknown packet
        /// </summary>
        Unknown = 0,
        
        /// <summary>
        /// New node are in network
        /// </summary>
        NewNode = 1,
        
        /// <summary>
        /// Lets block stay master, master is already present in network
        /// </summary>
        MasterBlock = 2,
        
        /// <summary>
        /// Packet with counter for control slaves
        /// </summary>
        CheckPacket = 3,
        
        /// <summary>
        /// Reply packet from slave
        /// </summary>
        CheckPacketResp = 4,
        
        /// <summary>
        /// Send info about new slave in network
        /// </summary>
        NewSlave = 5,
        
        /// <summary>
        /// Send self id for candidate for stay master
        /// </summary>
        MasterCandidate = 6,
        
        /// <summary>
        /// Init type of packet
        /// </summary>
        Init = 10,
        
        /// <summary>
        /// Stop by conflict Id
        /// </summary>
        ConflictIdStop = 11,
        
        /// <summary>
        /// Confirm start slave
        /// </summary>
        ConfirmSlave = 12,
        
        /// <summary>
        /// Command to prepare slave for get data
        /// </summary>
        PrepareSlave = 13,

        /// <summary>
        /// Slave is ready for get data
        /// </summary>
        SlaveReady = 14,

        /// <summary>
        /// Slave is in work
        /// </summary>
        SlaveOk = 15,

    }
}