using System;

namespace ReplicationModule
{
    public class NodeInfo
    {
        public long NodeId;
        public NodeState State;
        public NodeRole Role;
        public NodeStatus Status;
        public string NodeHostName;
        public string NodeHostIP;
        public DateTimeOffset LastActivityTime;
        public DateTimeOffset LastCheckTime;
        public CommandType LastPacketControl;
        public long LastPacketDestinationId;
        public long CheckCounter;
        public long CheckCounterPrev;
        public bool Processed;
        public int AnswerFailCount;
        public long TimeInRole;

    }
}