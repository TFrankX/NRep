using System;
using System.Collections.Generic;
using System.Text;

namespace MessageQueue
{
        public enum MessageType
        {
            /// <summary>
            /// Unknown (erroneous) type
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// Interface message
            /// </summary>
            InterfaceMessage = 1,

            /// <summary>
            /// Database with common data
            /// </summary>
            ReplicationMessage = 2,
            
            AskMessage = 3,
            BydMessage = 4,
    }

}
