using System;
using System.Collections.Generic;
using System.Text;

namespace MessageQueue
{
    public class Message
    {
        public long MessageId;
        public int NumMessageInQueue;
        public MessageType MsgType;
        public string MessageContext;
        public long Destination;
        public string Sender;
    }
}
