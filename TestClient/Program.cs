using System;
using System.Net;
using System.Threading;
using SimnetLib;
using SimnetLib.Network;

namespace TestClient
{
    class Program
    {

        public static void Main(string[] args)
        {
            var bus = new MQTTBus();
            var client = new SimnetClient(bus, "FirstClient");
            client.Connect("yaup.ru", 8884, "devclient", "Potato345!");

            Thread.Sleep(1000);


            client.Subscribe<CmdQueryNetworkInfo>("cabinet/Dev01/cmd/13", (sender, topic, message) =>
            {
                Console.WriteLine($"New command Info: {message.RlSeq}");
            });






            client.Subscribe<string>("hello/world", (sender, topic, message) =>
            {
                Console.WriteLine($"New Message: {message}");
            });


            Console.WriteLine("listening for messages!");
            Console.ReadKey(true);
        }
    }
}
