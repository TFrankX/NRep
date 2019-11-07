using System;
using System.Collections.Generic;
using System.Text;

namespace NatsProcess
{
 
    /// <summary>
    /// Message server interface
    /// </summary>
    public interface IMessageServer : IDisposable
    {

        /// <summary>
        /// Delegate for async subscription handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        public delegate void Handler(object sender, string message);

        /// <summary>
        /// Status of connection for push messages
        /// </summary>
        public bool ConnectedPush { get; }
        
        /// <summary>
        /// Status of connection for pop messages
        /// </summary>
        public bool ConnectedPop { get; }

        /// <summary>
        /// Set connection with server for pushing messages
        /// </summary>
        /// <returns>Result operation</returns>
        public bool SetConnectionPush();

        /// <summary>
        /// Set connection with server for getting messages
        /// </summary>
        /// <returns>Result operation</returns>
        public bool SetConnectionPop();

        /// <summary>
        /// Publish message to server
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Publish(string subject, object obj);

        /// <summary>
        /// Unsubscribe from subject
        /// </summary>
        /// <param name="subject"></param>
        /// <returns></returns>
        public bool Unsubscribe(string subject);

        /// <summary>
        /// Subscribe to the subject
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="handler"></param>
        /// <param name="synchronous"></param>
        /// <returns></returns>
        public bool Subscribe(string subject, Handler handler, bool synchronous);

       
    }
}
