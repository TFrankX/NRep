using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using NATS.Client;
using Newtonsoft.Json;
using NLog;

namespace NatsProcess
{

    /// <summary>
    /// Nats interaction class
    /// </summary>
    public class NatsProcessor : IMessageServer
    {
        public IConnection connectionPush;
        public IConnection connectionPop;
        private readonly ConnectionFactory connectionFactory;
        private readonly Options options;
      //  private delegate void Handler(object sender, string message);
        public Logger logger;
        public bool ConnectedPush => connectionPop?.State == ConnState.CONNECTED;
        public bool ConnectedPop => connectionPush?.State == ConnState.CONNECTED;
        private class Subscriber
        {
            public ISubscription Subscribe;
            public IMessageServer.Handler HandlerSubscribe;
            public Thread HandlerThread;
            public object Lock;
            public bool Synchronous;
        }
        private readonly Dictionary<string, Subscriber> subscribers=new Dictionary<string, Subscriber>();

       
        public NatsProcessor(string url = Defaults.Url, string creds = null)
        {
            //logger = LogManager.GetCurrentClassLogger();
            logger = LogManager.GetLogger("WpfTestApp.MainWindow");
            options = ConnectionFactory.GetDefaultOptions();
            options.AllowReconnect = true;
            options.ReconnectWait = 10000;
            options.Url = url;
            if (creds != null)
            {
                options.SetUserCredentials(creds);
            }
            connectionFactory = new ConnectionFactory();
        }
        /// <summary>
        /// Setting connection with Nats server for push messages
        /// </summary>
        public bool SetConnectionPush()
        {
            try
            {

                connectionPush = connectionFactory.CreateConnection(options);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"NATS Error: {ex.Message}");
                connectionPush = null;
                return false;
            }
        }
        /// <summary>
        /// Setting connection with Nats server for pop messages
        /// </summary>
        public bool SetConnectionPop()
        {
            try
            {
                connectionPop = connectionFactory.CreateConnection(options);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"NATS Error: {ex.Message}");
                connectionPop = null;
                return false;
            }
        }
        /// <summary>
        /// Publish message to Nats server
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Publish(string subject, object obj)
        {
            if (obj != null)
            {
                var sendPacket = Encoding.Default.GetBytes(JsonConvert.SerializeObject(obj));
                

                if (connectionPush == null)
                {
                    if (!SetConnectionPush())
                    {
                        //Console.WriteLine("Not connected");
                        logger.Error("NATS Error: Not connected");
                        return false;
                    }
                }
                try
                {
                    connectionPush.Publish(subject, sendPacket);
                    connectionPush.Flush();
                    return true;
                }
                catch (Exception ex)
                {
                    logger.Error($"NATS Error: {ex.Message}");
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Unsubscribe from subject
        /// </summary>
        /// <param name="subject"></param>
        /// <returns></returns>
        public bool Unsubscribe(string subject)
        {

            if (subscribers.ContainsKey(subject))
            {
                subscribers[subject].Subscribe.Unsubscribe();
                if (subscribers[subject].Synchronous)
                {
                    subscribers[subject].Lock = false;
                }
                else
                {
                    lock (subscribers[subject].Lock)
                    {
                        Monitor.Pulse(subscribers[subject].Lock);
                    }

                }
                subscribers.Remove(subject);
                Thread.Sleep(100);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Subscribe to the subject
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="handler"></param>
        /// <param name="synchronous"></param>
        /// <returns></returns>
        public  bool Subscribe(string subject, IMessageServer.Handler handler, bool synchronous)
        {
            if (subscribers?.Count > 0)
            {
                foreach (var line in subscribers)
                {
                    if (line.Key == subject)
                    {
                        logger.Error($"Error: subject {subject} is present");
                        return false;
                    }
                }

            }
            if (connectionPop == null)
            {
                if (!SetConnectionPop())
                {
                    Console.WriteLine("Not connected");
                    return false;
                }
            }
            try
            {
                subscribers?.Add(subject, new Subscriber
                {
                    HandlerSubscribe = handler,
                    HandlerThread = synchronous ? new Thread(DoSync) : new Thread(DoAsync),
                    Lock = synchronous ? true : new object(),
                    Synchronous = synchronous,
                });
                subscribers?[subject].HandlerThread.Start(subject);
                Thread.Sleep(10);
                return true;

            }
            catch (Exception ex)
            {
                logger.Error($"NATS Error: {ex.Message}");
                return false;
            }

        }
        /// <summary>
        /// Async thread for receiving
        /// </summary>
        /// <param name="obj"></param>
        private void DoAsync(object obj)
        {
            try
            {
                void MsgHandler(object sender, MsgHandlerEventArgs args) => subscribers[(string)obj].HandlerSubscribe
                    .Invoke(sender, Encoding.ASCII.GetString(args.Message.Data));
                subscribers[(string)obj].Subscribe=connectionPop.SubscribeAsync((string) obj, MsgHandler);
                lock (subscribers[(string) obj].Lock)
                {
                    Monitor.Wait(subscribers[(string) obj].Lock);
                }
            }
            catch (Exception ex)
            {
                subscribers.Remove((string)obj);
                logger.Error($"NATS Error: {ex.Message}");
            }
        }
        /// <summary>
        /// Sync thread for receiving
        /// </summary>
        /// <param name="obj"></param>
        private void DoSync(object obj)
        {
            try
            {
                using var subscribeSync = connectionPop.SubscribeSync((string) obj);
                while ((bool) subscribers[(string) obj].Lock)
                {
                    var m = subscribeSync.NextMessage();
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                subscribers.Remove((string) obj);
                logger.Error($"NATS Error: {ex.Message}");
            }
        }
        /// <summary>
        /// Gets statistic on subscribe channel
        /// </summary>
        /// <returns></returns>
        public string GetStatsPop()
        {
            return $"Statistics: Outgoing Bytes: {connectionPop.Stats.OutBytes};  Outgoing Messages: {connectionPop.Stats.OutMsgs};";
        }
        /// <summary>
        /// Get statistic for a pushing chanel
        /// </summary>
        /// <returns></returns>
        public string GetStatsPush()
        {
            return $"Statistics: Outgoing Bytes: {connectionPush.Stats.OutBytes};  Outgoing Messages: {connectionPush.Stats.OutMsgs};";
        }
        /// <summary>
        /// Dispose object
        /// </summary>
        public void Dispose()
        {
            foreach (var line in subscribers)
            {
                Unsubscribe(line.Key);
            }
            connectionPush?.Dispose(); 
            connectionPop?.Dispose();
        }

 
    }
}
