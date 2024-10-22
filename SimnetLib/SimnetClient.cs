using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using ProtoBuf;
using SimnetLib.Network;

namespace SimnetLib
{
    public delegate void MessageEventHandler<T>(object sender, string topic, T message);
    public delegate void ConnectedEventHandler(object sender);
    public delegate void DisconnectedEventHandler(object sender);
    public delegate void ConnectErrorEventHandler(object sender,string error);

    public class SimnetClient
    {
        private INetworkBus _networkBus;
        private Dictionary<string, Subscription> _subscriptions;
        public event ConnectedEventHandler EvConnected;
        public event DisconnectedEventHandler EvDisconnected;
        public event ConnectErrorEventHandler EvConnectError;

        public string ClientId { get; private set; }

        public SimnetClient(INetworkBus networkBus, string clientId)
        {
            _networkBus = networkBus;
            _subscriptions = new Dictionary<string, Subscription>();
            ClientId = clientId;
            _networkBus.Connected += NetworkBusOnConnected;
            _networkBus.Disconnected += NetworkBusOnDisconnected;
            _networkBus.ConnectError += NetworkBusOnConnectError;
        }


        public bool IsConnected()
        {
            return _networkBus.IsConnected();
        }

        public void Connect(string hostname, uint port, string username = "", string password = "", string certCA = "", string certCli="",string certPass="")
        {
            try
            {
                _networkBus.MessageReceived += NetworkBusOnMessageReceived;
                IPAddress host = Dns.GetHostAddresses(hostname)[0];
                _networkBus.Connect(host, (int)(port), ClientId, username, password,certCA,certCli,certPass);
            }
            catch (Exception e)
            {
                EvConnectError?.Invoke(this, e.Message);
            }
        }


        public void Connect(IPAddress host, int port)
        {
            try
            {
                _networkBus.MessageReceived += NetworkBusOnMessageReceived;
                _networkBus.Connect(host, port, ClientId);
            }
            catch (Exception e)
            {
                EvConnectError?.Invoke(this, e.Message);
            }
        }

        public void Subscribe<T>(string topic, MessageEventHandler<T> handler) where T : class
        {
            try
            {
                if (!_subscriptions.ContainsKey(topic))
                {
                    _subscriptions.Add(topic, new Subscription(typeof(T), handler));
                    _networkBus.Subscribe(topic);
                }
                else
                {
                    try
                    {
                        _networkBus.Subscribe(topic);
                        _subscriptions.Remove(topic);
                        _subscriptions.Add(topic, new Subscription(typeof(T), handler));
                        
                    }
                    catch { }
                }

            }
            catch (Exception e)
            {
                EvConnectError?.Invoke(this, e.Message);
            }

        }

        public void Publish<T>(string topic, T payload) where T : class
        {
            var data = new byte[0];

            // example, later use protobuf for serialisation
            if (typeof(T) == typeof(string))
            {
                data = Encoding.UTF8.GetBytes((string)(object)payload);
            }
            else if (typeof(T).IsProto())
            {
                // protobuf message
                using (var stream = new MemoryStream())
                {
                    Serializer.Serialize(stream, payload);
                    data = stream.ToArray();
                }
            }

            _networkBus.Publish(topic, data);
        }

        private void NetworkBusOnConnectError(object sender,string error)
        {
            EvConnectError?.Invoke(this,error);
        }

        private void NetworkBusOnConnected(object sender)
        {
            //handler?.DynamicInvoke

            EvConnected?.Invoke(this);
        }
        private void NetworkBusOnDisconnected(object sender)
        {
            EvDisconnected?.Invoke(this);
        }

        private void NetworkBusOnMessageReceived(object sender, string topic, byte[] payload)
        {
            //bool maskOk=false;
            string mask = "";
            try
            {
                foreach (var key in _subscriptions)
                {
                    if (key.ToString().Contains('#'))
                    {
                        mask = key.ToString().Substring(1, key.ToString().IndexOf('#') - 1);
                        if (topic.Contains(mask))
                        {
                            var subscriptionmask = _subscriptions[$"{mask}#"];
                            var handlermask = subscriptionmask.Delegate;
                            // example, later use protobuf for serialisation
                            object valuemask = null;
                            if (subscriptionmask.Type == typeof(string))
                            {
                                valuemask = Encoding.UTF8.GetString(payload);
                            }
                            else if (subscriptionmask.Type.IsProto())
                            {
                                // protobuf message
                                using (var stream = new MemoryStream(payload))
                                {
                                    valuemask = Serializer.Deserialize(stream, Activator.CreateInstance(subscriptionmask.Type));
                                }
                            }

                            handlermask?.DynamicInvoke(this, topic, valuemask);

                            break;
                        };
                    }
                }
            }
            catch
            {

            }
            if ((!_subscriptions.ContainsKey(topic))) return;
            
            var subscription = _subscriptions[topic];
           
            var handler = subscription.Delegate;

            // example, later use protobuf for serialisation
            object value = null;
            if (subscription.Type == typeof(string))
            {
                value = Encoding.UTF8.GetString(payload);
            }
            else if (subscription.Type.IsProto())
            {
                // protobuf message
                using (var stream = new MemoryStream(payload))
                {
                    try
                    {
                        value = Serializer.Deserialize(stream, Activator.CreateInstance(subscription.Type));
                    }
                    catch
                    {

                    }
                }
            }

            handler?.DynamicInvoke(this, topic, value);
        }
    }
}