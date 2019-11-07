using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using NATS.Client;
using NatsProcess;
using Newtonsoft.Json;
using NLog;
using QuintetLab.ExchangeEngine.Contracts.CryptoSpot.V1;


namespace MessageQueue
{
    public struct AckInfo
    {
        long IdNode;
        int IdMessage;
        int NumMessageInQueue;
        bool IsAcknowlege;
    }
    /// <summary>
    /// Messages process class
    /// </summary>
    public class MessagesProc
    {
        
        
        // Node Id        
        public long NodeId
        {
            get { return nodeId; }
            set { nodeId = value; }
        }
        // Stop process messages
        public bool StopProcessMessages

        {
            get { return stopProcessMessages; }
            set { stopProcessMessages = value; }
        }
        public NQueue<Message> OutMessagesQueue = new NQueue<Message>();
        public NQueue<Message> InMessagesQueue = new NQueue<Message>();
        public NQueue<AckMessage> AckMessagesQueue = new NQueue<AckMessage>();

      
        
        public IMessageServer messageServer;
        private bool isMasterMode;
        private long nodeId = 1;
        private bool messageProcRunning;
        private bool  stopProcessMessages=false;
        private readonly string masterSubject = "ForMaster01";
        private readonly string masterAckSubject = "ForMasterAck01";
        private readonly string replicaSubject = "Replica01";
        private List<AckInfo> ackInfo = new List<AckInfo>();
        public MessagesProc()
        {
            messageServer = new NatsProcessor();
            InMessagesQueue.IdAutoAssign = false;
            // Events for queue process
            InMessagesQueue.OnAddMessages += InMessagesProcess;
            OutMessagesQueue.OnAddMessages += OutMessagesProcess;
            AckMessagesQueue.OnAddMessages += AckMessagesProcess;
        }

        // Run queue processor
        public bool Run(bool IsMaster)
        {
            isMasterMode = IsMaster;
            if (messageProcRunning) { return false; }
            messageProcRunning = true;
            if (isMasterMode)
            {
                if ((!messageServer.Subscribe(masterSubject, MessageForMasterHandler, false)) ||
                   (!messageServer.Subscribe(masterAckSubject, MessageAckHandler, false)))
                {
                    return false;
                }
                return true;
            }
            else
            {
                if (!messageServer.Subscribe(replicaSubject, MessageForSlaveHandler, false))
                {
                    return false;
                }
                return true;
            }           
        }

        // Stop queue processor
        public void Stop()
        {
            if (!messageProcRunning) { return; }
            messageProcRunning = false;
            if (isMasterMode)
            {
                messageServer.Unsubscribe(masterSubject);                
            }
            else
            {
                messageServer.Unsubscribe(replicaSubject);
            }
        }

        // Messages handler for master
        void MessageForMasterHandler(object obj, string message)
        {
            var msg = JsonConvert.DeserializeObject<Message>(message);                  
            OutMessagesQueue.Push(msg);
        }
        // Messages handler for slave
        void MessageForSlaveHandler(object obj, string message)
        {
            var item = JsonConvert.DeserializeObject<NQueue<Message>.QueueItem>(message);
            if ((item.obj.Destination == 0) || (item.obj.Destination == nodeId))
            {
                InMessagesQueue.PushItem(item);
            }
        }
        // Acknowlegement message handler
        void MessageAckHandler(object obj, string message)
        {
            var item = JsonConvert.DeserializeObject<NQueue<AckMessage>.QueueItem>(message);
            

//            if ((item.obj.Destination == 0) || (item.obj.Destination == nodeId))
//            {
//            }
        
        
        }

        // Ackowlegement message processor
        void AckMessagesProcess(object sender, long numAddMessages)
        {



        }

        // In message processor
        void InMessagesProcess(object sender, long numAddMessages)
        {

            {
                if (isMasterMode) { return; }
                messageServer.Publish(masterAckSubject,
                                     new AckMessage
                                     {

                                     });
            }
        }
        // Out message processor
        void OutMessagesProcess(object sender, long numAddMessages)
        {         
            if (isMasterMode && !stopProcessMessages)
            {
                while (OutMessagesQueue.Count > 0)
                {                 
                    var Msg = OutMessagesQueue.PopItem();
                    var MessageObj = Msg.obj;
                    if (MessageObj != null)
                    {
                        InMessagesQueue.PushItem(Msg);
                        messageServer.Publish(replicaSubject, Msg);
                    }
                }
            }
        }
        // Copy messages from master to slaves
        public void CopyOutQueue(long id, long firstId)
        {
            stopProcessMessages = true;
            long destinationId = id;
            var listToCopy = new List<NQueue<Message>.QueueItem>(InMessagesQueue.items);

            foreach (var line in listToCopy)
            {
                if (line.IdNum > firstId)
                {
                    line.obj.Destination = destinationId;
                    messageServer.Publish(replicaSubject, line);
                }
            }
            stopProcessMessages = false;
            OutMessagesProcess(this, 0);
        }


 

        public void Dispose()
        {
            Stop();
            messageServer.Dispose();
        }

    }

}

