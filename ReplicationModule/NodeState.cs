namespace ReplicationModule
{
    public enum NodeState
    {
        /// <summary>
        /// Unknown stage
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Node power om
        /// </summary>
        NodePowerOn = 100,

        /// <summary>
        /// Node connected to NATS
        /// </summary>
        Connected = 105,

        /// <summary>
        /// Sending to all nodes info about self
        /// </summary>
        SendOnSignalToNodes = 110,

        /// <summary>
        /// Wait answer from master
        /// </summary>
        WaitingMasterAnswer = 120,

        /// <summary>
        /// Send conflict Id infos
        /// </summary>
        SendConflictIdInfo = 130,

        /// <summary>
        /// Stop node to conflict id permit
        /// </summary>
        ConflictIdStop = 140,

        /// <summary>
        /// Must stay slave
        /// </summary>
        StaySlave = 200,

        /// <summary>
        /// Wait a control packet from master
        /// </summary>
        WaitQueryMaster = 210,

        /// <summary>
        /// Wait after at least once a response to master
        /// </summary>
        WaitQueryMasterAfterSomeAsk = 215,


        /// <summary>
        /// Ask to master
        /// </summary>
        AskToMaster = 220,

        /// <summary>
        /// Not asks from master
        /// </summary>
        MasterNotQuery = 230,

        /// <summary>
        /// Send ask before stay master
        /// </summary>
        SendSelfMasterCandidate = 240,

        /// <summary>
        /// Waiting all candidates to stay master
        /// </summary>
        WaitingAllMasterCandidates = 250,

        /// <summary>
        /// Selecting the fastest candidate master
        /// </summary>
        SelectBestMaster = 260,


        /// <summary>
        /// Must stay master
        /// </summary>
        StayMaster = 300,


        /// <summary>
        /// Waiting timer to send check packet
        /// </summary>
        WaitToSendCheck = 310,

        /// <summary>
        /// Time to send check
        /// </summary>
        NeedSendCheck = 320,
        /// <summary>
        /// Send check packets for slaves
        /// </summary>
        CheckSlaves = 325,
        
        /// <summary>
        /// Waiting on answer from slave
        /// </summary>
        WaitSlaveAnswer = 330,

        /// <summary>
        /// Answer received
        /// </summary>
        SlaveAnswerOk = 340,

        /// <summary>
        /// Answer received
        /// </summary>
        SlaveNotAnswer = 350,

        /// <summary>
        /// Slave go down
        /// </summary>
        SlaveGoDown = 360,
        
        /// <summary>
        /// Waiting confirm from master
        /// </summary>
        WaitSlaveConfirmFromMaster = 370,

        /// <summary>
        /// Prepare slave to receive data
        /// </summary>
        SlavePrepare = 375,

        /// <summary>
        /// Slave confirmed from master
        /// </summary>
        SlaveConfirmed = 380,

        /// <summary>
        /// Not connected
        /// </summary>
        NotConnected = 400,

        /// <summary>
        /// Not connected
        /// </summary>
        ConnectRepeat = 410,

        /// <summary>
        /// New master in network
        /// </summary>
        NewMaster = 500,

        /// <summary>
        /// Receive command - MasterBlock, block to stay master
        /// </summary>
        MasterBlock = 510,

        /// <summary>
        /// Cannot start message queue 
        /// </summary>
        MessageQueueError = 600,

    }
}