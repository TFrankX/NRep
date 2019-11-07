using System;
using System.Collections.Generic;
using System.Text;

namespace MessageQueue
{
    
    public class AckMessage
    {
        public long MessageId;
        public int NumMessageInQueue;
        public DateTimeOffset SendTime;
        public bool AckInfo;
    }
}
