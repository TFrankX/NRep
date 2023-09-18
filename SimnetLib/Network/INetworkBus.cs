using System;
using System.Net;

namespace SimnetLib.Network
{
    public delegate void MessageReceivedEventHandler(object sender, string topic, byte[] payload);
    public delegate void ConnectedEventHandler(object sender);
    public delegate void DisconnectedEventHandler(object sender);
    public delegate void ConnectErrorEventHandler(object sender,string error);
    public interface INetworkBus
    {
        // todo: add more connect possibilities (with pw / cert usw.)
        bool IsConnected();
        void Connect(IPAddress host, int port, string clientId, string username = "", string password = "");

        // todo: add retain flag for publish
        // todo: add remove-retain support (retain with empty payload)

        void Publish(string topic, byte[] payload, MessageAssurance assurance = MessageAssurance.UnReliable);
        void Subscribe(string topic);

        event MessageReceivedEventHandler MessageReceived;
        event ConnectedEventHandler Connected;
        event DisconnectedEventHandler Disconnected;
        event ConnectErrorEventHandler ConnectError;
    }
}