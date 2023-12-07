using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
//using MQTTnet.Extensions.ManagedClient;
//using MQTTnet.Client.Connecting;
//using MQTTnet.Client.Options;
using MQTTnet.Protocol;

namespace SimnetLib.Network
{
    public class MQTTBus : INetworkBus
    {
        // todo: make all Ibus methods async!

        private IMqttClient _mqttClient;

        public MQTTBus()
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();
            _mqttClient.ConnectedAsync += e =>
            {
                Connected?.Invoke(this);
                return Task.CompletedTask;
            };

            _mqttClient.DisconnectedAsync += e =>
            {
                Disconnected?.Invoke(this);
                return Task.CompletedTask;
            };
           
        }

        public async void Connect(IPAddress host, int port, string clientId, string username = "", string password = "")
        {
            MqttClientOptions options;
            if (username == "")
            {
                options = new MqttClientOptionsBuilder()
               .WithTcpServer(host.ToString(), port)
               .WithClientId(clientId)
               .Build();
            }
            else
            {
                options = new MqttClientOptionsBuilder()
               .WithTcpServer(host.ToString(), port)
               .WithClientId(clientId)
               .WithCredentials(username, password)
               .Build();

            }
            MqttClientConnectResultCode resultCode;
            try
            {
                var result = await _mqttClient.ConnectAsync(options, CancellationToken.None);
                resultCode = result.ResultCode;
            } 
            catch (Exception ex)
            {
                resultCode = MqttClientConnectResultCode.ServerUnavailable;
                ConnectError.Invoke(this, ex.Message);
            }

            if (resultCode == MqttClientConnectResultCode.Success)
            {
              //  Console.WriteLine("mqtt: connected!");

                _mqttClient.ApplicationMessageReceivedAsync += e =>
                {
                    MessageReceived?.Invoke(this,
                        e.ApplicationMessage.Topic,
                        e.ApplicationMessage.Payload);
                        return Task.CompletedTask;
                };
                
                //_mqttClient.UseApplicationMessageReceivedHandler(e =>
                //{
                //    MessageReceived?.Invoke(this,
                //        e.ApplicationMessage.Topic,
                //        e.ApplicationMessage.Payload);
                //});
            }
            else
            {
               // Console.WriteLine("mqtt: could not connect");
            }
        }

        public async void Publish(string topic, byte[] payload, MessageAssurance assurance = MessageAssurance.UnReliable)
        {
           // if (assurance == MessageAssurance.Reliable)
            {
                try
                {
                   await Task.Run(() => _mqttClient.PublishAsync(
                      new MqttApplicationMessageBuilder()
                       .WithTopic(topic)
                       .WithPayload(payload)
                       .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                       .Build()
               ));

                }
                catch
                {
                    Disconnected?.Invoke(this);
                }

            }


            //else
            //{
               // await _mqttClient.PublishAsync(topic, payload);
            //}
        }

        public bool IsConnected()
        {
            return _mqttClient.IsConnected;
        }

        public async void Subscribe(string topic)
        {
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
        }

        public event MessageReceivedEventHandler MessageReceived;
        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;
        public event ConnectErrorEventHandler ConnectError;

    }
}