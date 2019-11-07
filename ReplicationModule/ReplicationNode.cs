using System;
using System.Collections.Generic;
using System.Linq;
using MessageQueue;
using Newtonsoft.Json;
using QuintetLab.ExchangeEngine.Contracts.CryptoSpot.V1;
using System.Threading;
using NatsProcess;
using NLog;

namespace ReplicationModule
{
    public class ReplicationNode
    {
        #region Properties

        /// <summary>
        /// State of current node
        /// </summary>
        public NodeState State { get => nodeState; }
        /// <summary>
        /// Role of current node
        /// </summary>
        public NodeRole Role { get => nodeRole; }

        /// <summary>
        /// Node id
        /// </summary>
        public long Id
        {
            get => id;
            set
            {
                if (!replicationRun ) {id = value;}
            }
        }

        /// <summary>
        /// Check counter of current node
        /// </summary>
        public long CheckCounter
        {
            get
            {
                if (nodes == null)
                {
                    return 0;
                }
                return nodes.ContainsKey(id) ? nodes[id].CheckCounter : 0;
            }
        }
        /// <summary>
        /// Info about all nodes from master
        /// </summary>
        public List<NodeInfo> NodesInfo
        {
            get { return nodesInfo; }
        }
        /// <summary>
        /// Timeout for repeat attempt connect to the server
        /// </summary>
        public int RepeatConnectTimeout
        {
            get { return repeatConnectTimeout; }
            set { repeatConnectTimeout = value; }
        }
        
        /// <summary>
        /// Timeout for master answer
        /// </summary>
        public int AnswerMasterTimeout
        {
            get { return answerMasterTimeout; }
            set { answerMasterTimeout = value; }
        }
        /// <summary>
        /// Timeout for slave answer
        /// </summary>
        public int AnswerSlaveTimeout
        {
            get { return answerSlaveTimeout; }
            set { answerSlaveTimeout = value; }
        }
        /// <summary>
        /// Timeout to repeat send check packet for slaves
        /// </summary>
        public int RepeatCheckPacketTimeout
        {
            get { return repeatCheckPacketTimeout; }
            set { repeatCheckPacketTimeout = value; }
        }
        /// <summary>
        /// Timeout how slave wait check packet from master
        /// </summary>
        public int SlaveWaitQueryTimeout
        {
            get { return slaveWaitQueryTimeout; }
            set { slaveWaitQueryTimeout = value; }
        }
        /// <summary>
        /// Count missed response from slave for set fail status to slave node
        /// </summary>
        public int AnswerFailMaxCount
        {
            get { return answerFailMaxCount; }
            set { answerFailMaxCount = value; }
        }
        /// <summary>
        /// Timeout for receive all master candidates
        /// </summary>
        public int WaitMasterCandidatesTimeout
        {
            get { return waitMasterCandidatesTimeout; }
            set { waitMasterCandidatesTimeout = value; }
        }
        /// <summary>
        /// Timeout for send info about all nodes to all slaves in network
        /// </summary>
        public int SendNodesInfoTimeout
        {
            get { return waitMasterCandidatesTimeout; }
            set { waitMasterCandidatesTimeout = value; }
        }
        /// <summary>
        /// Timeout for send info about all nodes to all slaves in network
        /// </summary>
        public bool KeepFailNodesInfo
        {
            get { return keepFailNodesInfo; }
            set { keepFailNodesInfo = value; }
        }

        #endregion
        private long id = 01;
        private int repeatConnectTimeout = 30000;
        private int answerMasterTimeout = 8000;
        private int answerSlaveTimeout = 3000;
        private int repeatCheckPacketTimeout = 5000;
        private int slaveWaitQueryTimeout = 15000;
        private int answerFailMaxCount = 5;
        private int answerFailCount = 0;
        private int connectFailCount = 0;
        private int waitMasterCandidatesTimeout = 1000;
        private int sendNodesInfoTimeout = 4000;
        private bool keepFailNodesInfo = false;
        private NodeState nodeState;
        private NodeStatus nodeStatus;
        ControlPacket controlPacketSend;
        public MessagesProc MessagesQueueProc;
        private readonly string ControlPacketSubjectName = "MEReplicationControlPacket";
        private readonly string NodesInfoPacketSubjectName = "MEReplicationNodesInfoPacket";
        private DateTimeOffset lastSendNodesInfoTime;
        private readonly IMessageServer messageServer;
        private NodeStatus nodeStatusPrev;
        private NodeState nodeStatePrev;
        private NodeRole nodeRole = NodeRole.Unknown;
        private NodeRole nodeRolePrev = NodeRole.Unknown;
        private DateTimeOffset timeStartRole;
        private readonly long nodeUid = DateTime.UtcNow.Ticks;
        private DateTimeOffset lastSendTime;
        private DateTimeOffset lastSendOnSignalTime;
        private DateTimeOffset lastSlavePrepareCommandTime; 
        private Dictionary<long, NodeInfo> nodes;
        private List<NodeInfo> nodesInfo;
        public delegate void ChangeStateHandler(object sender);
        public event ChangeStateHandler OnChangeState;
        public delegate void ChangeRoleHandler(object sender);
        public event ChangeRoleHandler OnChangeRole;
        public delegate void ChangeStatusHandler(object sender);
        public event ChangeStatusHandler OnChangeStatus;      
        private bool replicationRun;
        private List<NodeInfo> sendNodes;
        private List<long> removeList = new List<long>();
        private Dictionary<long, NodeInfo> addList = new Dictionary<long, NodeInfo>();
        private Logger logger;
        private Timer runTimer;
        /// <summary>
        /// Constructor for replication none
        /// </summary>
        public ReplicationNode ()
        {
            MessagesQueueProc = new MessagesProc();
            messageServer = new NatsProcessor();
            logger = LogManager.GetLogger("WpfTestApp.MainWindow");           
            runTimer = new Timer(MainReplicationVoid, null, Timeout.Infinite, Timeout.Infinite);
        }
        /// <summary>
        /// Run replication for this node
        /// </summary>
        public void  Run ()
        {
            if (replicationRun) return;           
            nodes = new Dictionary<long, NodeInfo>();
           // Start state for node
            nodeState = NodeState.NodePowerOn;
            // Add this node to info table
            nodes.Add(Id, new NodeInfo
            {
                NodeId = id,
                CheckCounter = 0,
                Role = NodeRole.Unknown,
                Status = NodeStatus.Unknown,
                NodeHostName = System.Environment.MachineName,
                NodeHostIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[0].ToString(),
                LastActivityTime = DateTimeOffset.Now,
                LastPacketControl = CommandType.Init,
            });
            replicationRun = true;
            runTimer.Change(100, Timeout.Infinite);             
        }
        /// <summary>
        /// Stop replivation node
        /// </summary>
        public void Stop()
        {
            if (!replicationRun) return;
            replicationRun = false;
            runTimer.Change(Timeout.Infinite, Timeout.Infinite);
            messageServer.Dispose();
            MessagesQueueProc.Stop();
            nodeRole = NodeRole.Unknown;
            nodeState = NodeState.Unknown;
            nodeStatus = NodeStatus.Unknown;
            timeStartRole = DateTimeOffset.Now;
            OnChangeState?.Invoke(this);
            OnChangeRole?.Invoke(this);
            OnChangeStatus?.Invoke(this);            
        }
        public void MainReplicationVoid(object obj)
        {
            {
                // Check conditions for generate events
                if (nodeStatePrev != nodeState)
                {
                    OnChangeState?.Invoke(this);
                }
                if (nodeRolePrev != nodeRole)
                {
                    OnChangeRole?.Invoke(this);
                }
                if (nodeStatusPrev != nodeStatus)
                {
                    OnChangeStatus?.Invoke(this);
                }
                // Store previous values
                nodeStatePrev = nodeState;
                nodeRolePrev = nodeRole;
                nodeStatusPrev = nodeStatus;
                nodes[Id].State = nodeState;
                nodes[Id].Role = nodeRole;
                nodes[Id].Status = nodeStatus;                    
                MessagesQueueProc.NodeId = id;
                // If keep flag = false, delete info about offline nodes from info table
                if (!keepFailNodesInfo)
                {
                    if (removeList.Count > 0)
                    {
                        foreach (var line in removeList)
                        {
                            nodes.Remove(line);
                        }
                    }
                    removeList.Clear();
                }
                // Add new nodes information to info table
                if (addList.Count > 0)
                {
                    foreach (var (key,value) in addList)
                    {
                        nodes.Add(key,value);
                    }
                    addList.Clear();
                }
                // Main case of replication algo
                switch (nodeState)
                {
                    // If node power on, let start
                    case NodeState.NodePowerOn:
                        nodeState = NodeState.NotConnected;
                        break;
                    // If node not connect, doing connection to server
                    case NodeState.NotConnected:
                        nodeRole = NodeRole.Unknown;
                        nodeStatus = NodeStatus.Unknown;
                        timeStartRole = DateTimeOffset.Now;
                        messageServer.SetConnectionPush();
                        messageServer.SetConnectionPop();
                        if ((messageServer.ConnectedPush) && (messageServer.ConnectedPop))
                        {
                            // Unsubscribe connections for try new connection
                            messageServer.Unsubscribe(ControlPacketSubjectName);
                            messageServer.Unsubscribe(NodesInfoPacketSubjectName);
                            nodeState = NodeState.Connected;
                            connectFailCount = 0;
                        }
                        else
                        {
                            // Increase the fail connect counter
                            connectFailCount++;
                            lastSendTime = DateTime.Now;
                            nodeState = NodeState.ConnectRepeat;
                        }
                        break;
                    // If time for new connect attemption, do it
                    case NodeState.ConnectRepeat:
                        if ((DateTime.Now.Ticks - lastSendTime.Ticks) / TimeSpan.TicksPerMillisecond > repeatConnectTimeout)
                        {
                            nodeState = NodeState.NotConnected;
                            connectFailCount = 0;
                        }                            
                        break;
                    // If connection success, subscribe on system subjects
                    case NodeState.Connected:
                        if (messageServer.Subscribe(ControlPacketSubjectName, ControlPacketHandler, false))
                        {
                            if (messageServer.Subscribe(NodesInfoPacketSubjectName, NodeInfoPacketHandler, false))
                            {
                                nodeState = NodeState.SendOnSignalToNodes;                                  
                                nodeStatus = NodeStatus.Ok;
                            }
                        }
                        else nodeState = NodeState.NotConnected;
                        break;
                    // Send signal to all nodes about self (new node)
                    case NodeState.SendOnSignalToNodes:
                        timeStartRole = DateTimeOffset.Now;
                        controlPacketSend = new ControlPacket()
                        {
                            DestinationId = 0,
                            SourceHostName = System.Environment.MachineName,
                            SourceHostIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[0].ToString(),
                            SourceState = NodeState.WaitingMasterAnswer,
                            SourceRole = nodeRole,
                            SourceId = id,
                            SourceUid = nodeUid,
                            CheckCounter = 0,
                            TimeInRole = DateTimeOffset.Now.Ticks - timeStartRole.Ticks,
                            Control = CommandType.NewNode,
                        };
                        if (messageServer.Publish(ControlPacketSubjectName, controlPacketSend))
                        {                              
                            if (nodeState == NodeState.SendOnSignalToNodes)
                            {
                                nodeState = NodeState.WaitingMasterAnswer;
                                lastSendOnSignalTime = DateTimeOffset.Now;
                            }                                
                        }
                        else nodeState = NodeState.NotConnected;
                        lastSendTime = DateTime.Now;
                        break;
                    // Wait answer from master node
                    case NodeState.WaitingMasterAnswer:
                        if ((DateTime.Now.Ticks - lastSendOnSignalTime.Ticks) / TimeSpan.TicksPerMillisecond > answerMasterTimeout)
                        {
                            // No answer? Let stay master
                            nodeState = NodeState.StayMaster;
                        }
                        break;
                    // Staying master                
                    case NodeState.StayMaster:
                        timeStartRole = DateTimeOffset.Now;
                        nodeRole = NodeRole.Master;
                        logger.Info("Stay master");
                        // Restart message processor for subscribe in master mode
                        MessagesQueueProc.Stop();
                        if (!MessagesQueueProc.Run(true)) 
                        {
                            nodeState = NodeState.MessageQueueError;
                            Stop();
                        };
                        // State - checking slave nodes
                        nodeState = NodeState.CheckSlaves;
                        // Add info about master node to info list
                        foreach (var (_,value) in nodes)
                        {
                            if ((value.Role == NodeRole.Master) && (value.NodeId != id))
                            {
                                removeList.Add(value.NodeId);
                            }
                        }
                        break;
                        
                    //Send to slaves check packets and wait answers
                    case NodeState.CheckSlaves:                           
                        foreach (var (_, value) in nodes)
                        {
                            //If node = slave, processed it
                            if (value.Role == NodeRole.Slave)
                            {
                                //If time for send packet
                                if ((value.Processed) &&
                                    ((DateTime.Now.Ticks -
                                        value.LastCheckTime.Ticks) > repeatCheckPacketTimeout * TimeSpan.TicksPerMillisecond))
                                {
                                    //Send check packet
                                    controlPacketSend = new ControlPacket()
                                    {
                                        DestinationId = value.NodeId,
                                        SourceHostName = System.Environment.MachineName,
                                        SourceHostIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[0].ToString(),
                                        SourceState = NodeState.WaitSlaveAnswer,
                                        SourceRole = nodeRole,
                                        SourceStatus = nodeStatus,
                                        SourceId = id,
                                        CheckCounter = value.CheckCounter,
                                        Control = CommandType.CheckPacket,
                                    };
                                    value.LastPacketControl = CommandType.CheckPacket;
                                    value.CheckCounterPrev = value.CheckCounter;
                                    if (!messageServer.Publish(ControlPacketSubjectName, controlPacketSend))
                                    {
                                        nodeState = NodeState.NotConnected;
                                        break;
                                    }
                                    value.LastCheckTime = DateTimeOffset.Now;
                                    value.Processed = false; //Start check control
                                }                                   
                                //If check control is open
                                if (!value.Processed)
                                {
                                    //If time for response not elapsed an new packet received
                                        if ((DateTime.Now.Ticks - value.LastActivityTime.Ticks) <
                                        answerSlaveTimeout * TimeSpan.TicksPerMillisecond)
                                        {
                                        if (value.LastPacketControl == CommandType.CheckPacketResp)
                                        {
                                            if (value.CheckCounter == value.CheckCounterPrev + 1)
                                            {
                                                value.State = NodeState.SlaveAnswerOk;
                                                value.AnswerFailCount = 0;
                                                value.Processed = true;
                                            }
                                            else
                                            {
                                                value.State = NodeState.SlaveNotAnswer;
                                                value.AnswerFailCount++;
                                                value.Processed = true;
                                            }
                                        }
                                        }
                                    else
                                    {
                                        //If time is end and no receive packet
                                        if ( (value.LastPacketControl == CommandType.CheckPacket))
                                        {
                                            value.State = NodeState.SlaveNotAnswer;
                                            value.AnswerFailCount++;
                                        }
                                        // If fail-answer counter above then limit+1, delete offline node
                                        if (value.AnswerFailCount > answerFailMaxCount + 1)
                                        {
                                            value.AnswerFailCount = 0;
                                            removeList.Add(value.NodeId);
                                        }
                                        // if fail-answer counter above then limit, set status SlaveGoDown
                                        if (value.AnswerFailCount > answerFailMaxCount)
                                        {
                                            if (value.State != NodeState.SlaveGoDown)
                                            {
                                                value.Status = NodeStatus.Fail;
                                                value.State = NodeState.SlaveGoDown;
                                            }
                                        }
                                        value.Processed = true;
                                    }
                                }
                            }
                        }
                        break;
                    // If new master in network, let stay slave
                    case NodeState.NewMaster:
                        nodeState = NodeState.StaySlave;
                        break;
                    // Let stay slave
                    case NodeState.StaySlave:
                        nodeRole = NodeRole.Unknown;
                        timeStartRole = DateTimeOffset.Now;
                        logger.Info("Stay slave");
                        MessagesQueueProc.Stop();
                        // Send request to master node
                        controlPacketSend = new ControlPacket()
                        {
                            DestinationId = 0,
                            SourceHostName = System.Environment.MachineName,
                            SourceHostIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[0].ToString(),
                            SourceState = nodeState,
                            SourceRole = nodeRole,
                            SourceId = id,
                            CheckCounter = 0,
                            TimeInRole = DateTimeOffset.Now.Ticks - timeStartRole.Ticks,
                            Control = CommandType.NewSlave,                                
                        };                        
                        if (messageServer.Publish(ControlPacketSubjectName, controlPacketSend))
                        {
                            nodeState = NodeState.WaitSlaveConfirmFromMaster;
                                answerFailMaxCount = 0;
                            lastSlavePrepareCommandTime = DateTimeOffset.Now;                            
                        }
                        else
                        {
                            nodeState = NodeState.NotConnected;
                            break;
                        }
                        break;
                // Wait answer from master to prepare node for receive info
                    case NodeState.WaitSlaveConfirmFromMaster:                            
                        if ((DateTimeOffset.Now.Ticks - lastSlavePrepareCommandTime.Ticks) / TimeSpan.TicksPerMillisecond > answerMasterTimeout)
                        {                                                                                        
                            // if fail-counter less then max, try request to master again
                            if (answerFailCount < answerFailMaxCount)
                            { 
                                controlPacketSend = new ControlPacket()
                                {
                                    DestinationId = 0,
                                    SourceHostName = System.Environment.MachineName,
                                    SourceHostIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[0].ToString(),
                                    SourceState = nodeState,
                                    SourceRole = nodeRole,
                                    SourceId = id,
                                    CheckCounter = 0,
                                    TimeInRole = DateTimeOffset.Now.Ticks - timeStartRole.Ticks,
                                    Control = CommandType.NewSlave,

                                };
                                if (messageServer.Publish(ControlPacketSubjectName, controlPacketSend))
                                {
                                    lastSlavePrepareCommandTime = DateTimeOffset.Now;
                                }                                
                                else
                                {                                        
                                    nodeState = NodeState.NotConnected;
                                    break;
                                }
                                answerFailCount++;
                            } 
                            else
                            {
                                // If master not ask, go initialisation
                                answerFailCount = 0;
                                nodeState = NodeState.SendOnSignalToNodes;
                            }
                        }
                        break;
                    // Master send all info and confirm slave
                    case NodeState.SlaveConfirmed:
                        // Go waiting queries from master
                        nodeState = NodeState.WaitQueryMaster;
                    break;
                    // Wait query from master
                    case NodeState.WaitQueryMaster:
                        //If time is end, control for past queries
                        if ((DateTimeOffset.Now.Ticks - nodes[id].LastCheckTime.Ticks) >= slaveWaitQueryTimeout * TimeSpan.TicksPerMillisecond)
                        {
                            nodeRole = NodeRole.Unknown;
                            timeStartRole = DateTimeOffset.Now;
                        //If no queries - send info for masters candidate
                        if  (nodeState == NodeState.WaitQueryMaster)
                            {
                                nodeState = NodeState.SendSelfMasterCandidate;                                    
                            }
                            else
                            {
                                nodeState = NodeState.AskToMaster;
                            }
                        }                            
                        break;
                            
                    //If asked to master, wait next query
                    case NodeState.AskToMaster:
                        nodeState = NodeState.WaitQueryMaster;
                        break;
                    //Send self-info to Master candidate
                    case NodeState.SendSelfMasterCandidate:
                        controlPacketSend = new ControlPacket()
                        { 
                            DestinationId = 0,
                            SourceHostName = System.Environment.MachineName,
                            SourceHostIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[0].ToString(),
                            SourceState = nodeState,
                            SourceRole = nodeRole,
                            SourceId = id,
                            CheckCounter = 0,
                            PacketTime  = DateTimeOffset.Now,
                            Control = CommandType.MasterCandidate,
                            TimeInRole = DateTimeOffset.Now.Ticks - timeStartRole.Ticks,

                        };
                        if (!messageServer.Publish(ControlPacketSubjectName, controlPacketSend))
                        {
                            nodeState = NodeState.NotConnected;
                            lastSendOnSignalTime = DateTimeOffset.Now;
                            break;
                        }
                        lastSendTime = DateTime.Now;
                        nodeState = NodeState.WaitingAllMasterCandidates;
                        break;
                    //Wait info about all candidates to master
                    case NodeState.WaitingAllMasterCandidates:
                        if (((DateTime.Now.Ticks  - lastSendOnSignalTime.Ticks) / TimeSpan.TicksPerMillisecond > waitMasterCandidatesTimeout))
                        {
                            nodeState = NodeState.SelectBestMaster;
                        }
                        break;
                    //Select the best master candidate
                    case NodeState.SelectBestMaster:
                        //Find master candidate with min id
                        long minIndex = id;
                        foreach (var (key, value) in nodes)
                        {
                            if ((!value.Processed) && (value.LastPacketControl == CommandType.MasterCandidate))
                            {
                                if (value.NodeId < minIndex)
                                {
                                    minIndex = value.NodeId;
                                }
                            }
                            value.Processed = true;
                        }
                        if (minIndex == id)
                        {
                            //Best candidate - this node
                            nodeState = NodeState.SendOnSignalToNodes;
                        }
                        else
                        {
                            //Best candidate another node, stay slave
                            nodeState = NodeState.SlaveConfirmed;
                        }
                        break;
                    // Stop by conflict id
                    case NodeState.ConflictIdStop:
                        break;
                }
                // Send infos about all nodes to the slaves
                if (nodeRole == NodeRole.Master)
                {
                    if ((DateTimeOffset.Now.Ticks - lastSendNodesInfoTime.Ticks) / TimeSpan.TicksPerMillisecond >
                        sendNodesInfoTimeout)
                    {
                        sendNodes = new List<NodeInfo>();
                        foreach (var (_, value) in nodes)
                        {
                            sendNodes.Add(value);
                        }
                        messageServer.Publish(NodesInfoPacketSubjectName, sendNodes);
                        lastSendNodesInfoTime = DateTimeOffset.Now;
                    }
                    
                }
                // Run replication cycle again, but not early, than 100ms
                runTimer.Change(100,Timeout.Infinite);
            }  
    
        }

        /// <summary>
        /// Receive nodes packet information handle
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="message"></param>
        private void ControlPacketHandler(object obj, string message)
        {
            ControlPacket controlPacketSend;
            // Get object from message
            var controlPacketReceive = !string.IsNullOrEmpty(message)                
                ? JsonConvert.DeserializeObject<ControlPacket>(message)
                : null;

            // If receive empty packet, then exit
            if (controlPacketReceive == null)
            {
                return;
            }
            logger.Info($"Packet: {controlPacketReceive?.Control}");
            
            // If packet not for this node or not for all nodes, exit
            if ((controlPacketReceive.DestinationId != id) &&
                (controlPacketReceive.DestinationId != 0))
            {
                return;
            }

            // If get conflict info packet, stop immediatly
            if ((controlPacketReceive?.Control == CommandType.ConflictIdStop))
            {
                if (controlPacketReceive.TimeInRole > DateTimeOffset.Now.Ticks - timeStartRole.Ticks)
                {
                    nodeState = NodeState.ConflictIdStop;
                    Stop();                    
                    return;
                }
            }

            // If new node in network with same id, conflict with this node, send conflict packet to it
            if ((controlPacketReceive.SourceId == id)  && (controlPacketReceive?.Control == CommandType.NewNode) && (controlPacketReceive.SourceUid != nodeUid) &&
                            (nodeState != NodeState.SendOnSignalToNodes))
            {
                controlPacketSend = new ControlPacket()
                {
                    DestinationId = 0,
                    SourceHostName = System.Environment.MachineName,
                    SourceHostIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[0].ToString(),
                    SourceState = nodeState,
                    SourceRole = nodeRole,
                    SourceId = id,
                    CheckCounter = 0,
                    TimeInRole = DateTimeOffset.Now.Ticks - timeStartRole.Ticks,
                    Control = CommandType.ConflictIdStop,
                };
                if (!messageServer.Publish(ControlPacketSubjectName, controlPacketSend))
                {
                    nodeState = NodeState.NotConnected;
                }

                return; //exit, in order to restrict storing info about conflict node
            }

            // If nodes info-list not contain info about packet source node, add this node to the add-list
            if (!nodes.ContainsKey(controlPacketReceive.SourceId))
            {
                addList.Add(controlPacketReceive.SourceId, new NodeInfo
                {
                    NodeId = controlPacketReceive.SourceId,
                    CheckCounter = controlPacketReceive.CheckCounter,
                    Role = controlPacketReceive.SourceRole,
                    // State = controlPacketReceive.SourceState,
                    NodeHostIP = controlPacketReceive.SourceHostIP,
                    NodeHostName = controlPacketReceive.SourceHostName,
                    LastActivityTime = DateTimeOffset.Now,
                    LastPacketControl = controlPacketReceive.Control,
                    LastPacketDestinationId = controlPacketReceive.DestinationId,
                    TimeInRole = controlPacketReceive.TimeInRole,
                    Processed = false,
                }); ;
            }
            else //If node-info present in list, lets update it
            {

                // Not update counter, if packet different from 'check' type
                nodes[controlPacketReceive.SourceId].NodeId = controlPacketReceive.SourceId;
                if (((controlPacketReceive.Control == CommandType.CheckPacket) ||
                     (controlPacketReceive.Control == CommandType.CheckPacketResp)) &&
                    (controlPacketReceive.SourceId != id))
                {
                    nodes[controlPacketReceive.SourceId].CheckCounter = controlPacketReceive.CheckCounter;
                }

                nodes[controlPacketReceive.SourceId].Role = controlPacketReceive.SourceRole;                
                nodes[controlPacketReceive.SourceId].NodeHostIP = controlPacketReceive.SourceHostIP;
                nodes[controlPacketReceive.SourceId].NodeHostName = controlPacketReceive.SourceHostName;
                nodes[controlPacketReceive.SourceId].LastActivityTime = DateTimeOffset.Now;
                nodes[controlPacketReceive.SourceId].LastPacketControl = controlPacketReceive.Control;
                nodes[controlPacketReceive.SourceId].LastPacketDestinationId = controlPacketReceive.DestinationId;
                nodes[controlPacketReceive.SourceId].TimeInRole = controlPacketReceive.TimeInRole;
                nodes[controlPacketReceive.SourceId].Processed = false;
            }
            // If get master-block packet, go stay slave
            if ((controlPacketReceive?.Control == CommandType.MasterBlock) && (controlPacketReceive?.SourceId != id) && ((nodeState == NodeState.WaitingMasterAnswer) || (nodeState == NodeState.SendOnSignalToNodes) || (nodeRole == NodeRole.Master)))
            {             
                {
                    nodeState = NodeState.StaySlave;
                    return;
                }
            }

            // If get request to new slave
            if ((controlPacketReceive?.Control == CommandType.NewSlave) && (nodeRole == NodeRole.Master) && (controlPacketReceive?.SourceId != id))
            {                 
                // Prepeare to send info to new slave, stop queue process, then stopping public replication info to slaves
                 MessagesQueueProc.StopProcessMessages = true;
                 controlPacketSend = new ControlPacket()
                {
                    DestinationId = controlPacketReceive.SourceId,
                    SourceHostName = System.Environment.MachineName,
                    SourceHostIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[0].ToString(),
                    SourceState = nodeState,
                    SourceRole = nodeRole,
                    SourceId = id,
                    CheckCounter = 0,
                    Control = CommandType.PrepareSlave,
                };
                if (!messageServer.Publish(ControlPacketSubjectName, controlPacketSend))
                {
                    nodeState = NodeState.NotConnected;
                }
                lastSendTime = DateTime.Now;

                return;
            }
            // Slave get prepeare command
            if ((controlPacketReceive?.Control == CommandType.PrepareSlave) && (nodeRole == NodeRole.Unknown) && (controlPacketReceive?.SourceId != id))
            {
                // Start message process, to receive info from master               
                if (!MessagesQueueProc.Run(false))
                {
                    nodeState = NodeState.MessageQueueError;
                    Stop();
                };
                controlPacketSend = new ControlPacket()
                {
                    DestinationId = controlPacketReceive.SourceId,
                    SourceHostName = System.Environment.MachineName,
                    SourceHostIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[0].ToString(),
                    SourceState = nodeState,
                    SourceRole = nodeRole,
                    SourceId = id,
                    CheckCounter = 0,
                    Control = CommandType.SlaveReady,
                    TimeInRole = DateTimeOffset.Now.Ticks - timeStartRole.Ticks,
                    QueueInfo = new QueuesInfo { LastInputMessageId = MessagesQueueProc.InMessagesQueue.LastPushId },
                };
                if (!messageServer.Publish(ControlPacketSubjectName, controlPacketSend))
                {
                    nodeState = NodeState.NotConnected;
                }
                return;
            }
            // If slave ready, send all info to it
            if ((controlPacketReceive?.Control == CommandType.SlaveReady) && (nodeRole == NodeRole.Master) && (controlPacketReceive?.SourceId != id))
            {
                Thread t = new Thread(new ParameterizedThreadStart(CopyQueue));
                t.Start(controlPacketReceive);
                return;
            }

            // If slave get confirm packet, send 'ok' packet to master
            if ((controlPacketReceive?.Control == CommandType.ConfirmSlave) && (nodeRole == NodeRole.Unknown) && (controlPacketReceive?.SourceId != id))
            {
                nodeRole = NodeRole.Slave;
                timeStartRole = DateTimeOffset.Now;
                nodes[id].LastCheckTime = DateTimeOffset.Now;
                nodeState = NodeState.WaitQueryMaster;
                controlPacketSend = new ControlPacket()
                {
                    DestinationId = controlPacketReceive.SourceId,
                    SourceHostName = System.Environment.MachineName,
                    SourceHostIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[0].ToString(),
                    SourceState = nodeState,
                    SourceRole = nodeRole,
                    SourceId = id,
                    CheckCounter = 0,
                    Control = CommandType.SlaveOk,
                    TimeInRole = DateTimeOffset.Now.Ticks - timeStartRole.Ticks,
                    QueueInfo = new QueuesInfo { LastInputMessageId = MessagesQueueProc.InMessagesQueue.LastPushId },
                };
                if (!messageServer.Publish(ControlPacketSubjectName, controlPacketSend))
                {
                    nodeState = NodeState.NotConnected;
                    return;
                }
                nodeState = NodeState.SlaveConfirmed;
                return;
            }

            // If new node in network, send master-block packet to it to stay it slave
            if (controlPacketReceive?.Control == CommandType.NewNode)
            {
                if (controlPacketReceive.SourceId != id)
                {
                    if ((nodeRole == NodeRole.Master) || (nodeState == NodeState.WaitingMasterAnswer) )
                    {                        
                        controlPacketSend = new ControlPacket()
                        {
                            DestinationId = controlPacketReceive.SourceId,
                            SourceHostName = System.Environment.MachineName,
                            SourceHostIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[0].ToString(),
                            SourceState = nodeState,
                            SourceRole = nodeRole,
                            SourceId = id,
                            CheckCounter = 0,
                            Control = CommandType.MasterBlock,
                        };
                        if (!messageServer.Publish(ControlPacketSubjectName, controlPacketSend))
                        {
                            nodeState = NodeState.NotConnected;
                        }
                        else 
                        lastSendTime = DateTime.Now;
                    }
                }
                return;
            }
            // If master get master candidate packet, send master-block packet to it source node to stay it slave
            if ((controlPacketReceive?.Control == CommandType.MasterCandidate) &&
                (controlPacketReceive?.SourceId != id))
            {
                if ((nodeRole == NodeRole.Master) || (nodeState == NodeState.WaitingMasterAnswer))
                {
                    controlPacketSend = new ControlPacket()
                    {
                        DestinationId = controlPacketReceive.SourceId,
                        SourceHostName = System.Environment.MachineName,
                        SourceHostIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[0].ToString(),
                        SourceState = nodeState,
                        SourceRole = nodeRole,
                        SourceId = id,
                        CheckCounter = 0,
                        Control = CommandType.MasterBlock,
                    };
                    if (!messageServer.Publish(ControlPacketSubjectName, controlPacketSend))
                    {
                        nodeState = NodeState.NotConnected;
                    }
                    else
                        lastSendTime = DateTime.Now;
                }
                else if (nodeState != NodeState.WaitingAllMasterCandidates)
                {
                    nodeState = NodeState.SendSelfMasterCandidate;
                }
                return;
            }

            //Received a check packet
            if (controlPacketReceive?.Control == CommandType.CheckPacket)
            {
                //Send answer to master
                controlPacketSend = new ControlPacket()
                {
                    DestinationId = controlPacketReceive.SourceId,
                    SourceHostName = System.Environment.MachineName,
                    SourceHostIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[0].ToString(),
                    SourceState = NodeState.AskToMaster,
                    SourceRole = nodeRole,
                    SourceId = id,
                    CheckCounter = controlPacketReceive.CheckCounter + 1,
                    Control = CommandType.CheckPacketResp,
                    PacketTime = DateTimeOffset.Now,
                };
                if (!messageServer.Publish(ControlPacketSubjectName, controlPacketSend))
                {
                    nodeState = NodeState.NotConnected;
                }
                lastSendTime = DateTime.Now;

                //Refresh state of slave node
                nodes[id].CheckCounterPrev = nodes[id].CheckCounter;
                nodes[id].CheckCounter = controlPacketReceive.CheckCounter;
                nodes[id].LastCheckTime = DateTimeOffset.Now;
                nodes[id].Processed = true;
                return;
            }
        }

        // Copy all missing messages to slave from master
        private void CopyQueue(object obj)
        {
            var controlPacketReceive = (ControlPacket)obj;
            ControlPacket controlPacketSend;
            MessagesQueueProc.CopyOutQueue(controlPacketReceive.SourceId,
            controlPacketReceive.QueueInfo.LastInputMessageId);
            MessagesQueueProc.StopProcessMessages = false;
            controlPacketSend = new ControlPacket()
            {
                DestinationId = controlPacketReceive.SourceId,
                SourceHostName = System.Environment.MachineName,
                SourceHostIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[0].ToString(),
                SourceState = nodeState,
                SourceRole = nodeRole,
                SourceId = id,
                CheckCounter = 0,
                Control = CommandType.ConfirmSlave,
            };
            if (!messageServer.Publish(ControlPacketSubjectName, controlPacketSend))
            {
                nodeState = NodeState.NotConnected;
            }
            lastSendTime = DateTime.Now;
        }
        
        // If receive info packet, update nodes info list
        public void NodeInfoPacketHandler(object obj, string message)
        {
            ControlPacket controlPacketSend;
            nodesInfo = !string.IsNullOrEmpty(message) ? JsonConvert.DeserializeObject<List<NodeInfo>>(message) : null;
            foreach (var line in nodesInfo)
            {
                if ((line.Role == NodeRole.Master) && (nodeRole == NodeRole.Master) && (line.NodeId != id))
                {                    
                    controlPacketSend = new ControlPacket()
                    {
                        DestinationId = 0,
                        SourceHostName = System.Environment.MachineName,
                        SourceHostIP = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[0].ToString(),
                        SourceState = nodeState,
                        SourceRole = nodeRole,
                        SourceId = id,
                        CheckCounter = 0,
                        Control = CommandType.MasterBlock,
                    };
                    if (!messageServer.Publish(ControlPacketSubjectName, controlPacketSend))
                    {
                        nodeState = NodeState.NotConnected;
                    }
                    else
                        lastSendTime = DateTime.Now;
                    return;
                }
            }

        }

        public void Dispose()
        {
          Stop();
            messageServer.Dispose();
          MessagesQueueProc.Dispose();
        }




    }




}
