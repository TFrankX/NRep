using System;


namespace ReplicationModule
{
    public class ControlPacket
    {
        public long DestinationId;
        public NodeState SourceState;
        public NodeRole SourceRole;
        public NodeStatus SourceStatus;
        public string SourceHostName;
        public string SourceHostIP;
        public long SourceId;
        public long SourceUid;
        public long CheckCounter;
        public CommandType Control;
        public DateTimeOffset PacketTime;
        public long TimeInRole;
        public QueuesInfo QueueInfo;

    }
}