using System;
using System.Collections.Generic;
using System.Text.Json;
using HtmlAgilityPack;
using MessageQueue;
using QuintetLab.ExchangeEngine.Contracts.CryptoSpot.V1;
using QuintetLab.MatchingEngine.Contracts.CryptoSpot.V1;
using NatsProcess;


namespace TestReplication
{
    class Program
    {
        static void Main(string[] args)
        {
            // HtmlWeb w = new HtmlWeb();
            // var hd = w.Load("https://www.gismeteo.ru/");
            // var frame = hd.DocumentNode.SelectSingleNode("//frame[*]");
            // var cities = hd.DocumentNode.SelectNodes("//a[@href]");
            //frame[@name='list']"

            /*            
                        DumpUtils dumpUtils = new DumpUtils();
                        FakeOrder fakeOrder = new FakeOrder();
                        OrderBook HostState = new OrderBook();
                        HostState.OrderBooks = new OrderBook.OrderBookData[10];
                        HostState.OrderBooks[0] = new OrderBook.OrderBookData
                        {
                            /*
                                            Ask = new CryptoOrder[1]{new CryptoOrder
                                            {
                                                InstrumentId = 1,
                                                ChildOrderId = 2,
                                                CreationTimestamp = new DateTimeOffset(2019, 10, 04, 12, 36, 00, TimeSpan.Zero),
                                                LastUpdateTimestamp = new DateTimeOffset(2019, 10, 04, 13, 36, 00, TimeSpan.Zero),
                                                OrderType = CryptoOrderType.Limit,
                                                ParentOrderId = 2,
                                                Price = 12.2m,
                                                RemainingVolume = 3.2m,
                                                Settings = new JsonElement(),
                                                Status = 0,
                                            }},
                            */
            /*          
                          Ask = new CryptoOrder[3]{fakeOrder.GetNew(), fakeOrder.GetNew(), fakeOrder.GetNew()},
                          Bid = new CryptoOrder[3]{ fakeOrder.GetNew(), fakeOrder.GetNew(), fakeOrder.GetNew()},
                          Instrument = new CryptoInstrument(),
                          MessageQueue = new Queue<string>(),
                          Status = new OrderBook.OrderBookStatus(),
                      };
                      dumpUtils.SaveDump(HostState);
                      Console.WriteLine($"Z={HostState.OrderBooks[0].Ask[1].Price}");
          */
            
            
/*            
            MessagesProc MsqProcess = new MessagesProc();
            MsqProcess.Run();
            System.Threading.Thread.Sleep(2000);
            MsqProcess.PushMessage(new Message()
            {
                Destination = "all",
                MessageContext = "Fucking shit",
                MessageId = 0,
                MsgType = MessageType.AskMessage,
                Sender = "Go hell",
            });
            System.Threading.Thread.Sleep(2000);
            MsqProcess.PushMessage(new Message()
            {
                Destination = "all",
                MessageContext = "Bitch!",
                MessageId = 0,
                MsgType = MessageType.AskMessage,
                Sender = "thread",
            });
            System.Threading.Thread.Sleep(3000);


            MsqProcess.PushMessage(new Message()
            {
                Destination = "all",
                MessageContext = "Fuck!!!",
                MessageId = 0,
                MsgType = MessageType.AskMessage,
                Sender = "thread",
            });
            
    
   */
            NatsProcessor natsProcess = new NatsProcessor();
            /*           
                        natsProcess.Subscribe("Replica01", MsgHandler1, false);
                        natsProcess.Subscribe("Replica02", MsgHandler2, false);
                        System.Threading.Thread.Sleep(5000);
                        natsProcess.Publish("Replica01", "Simin1");
                        System.Threading.Thread.Sleep(5000);
                        natsProcess.Publish("Replica02", "Pimin1");

                        natsProcess.Unsubscribe("Replica01");
                        natsProcess.Publish("Replica01", "Simin2");
                        natsProcess.Publish("Replica02", "Pimin2");
                        natsProcess.Subscribe("Replica01", MsgHandler1, false);
                        System.Threading.Thread.Sleep(5000);

                        natsProcess.Publish("Replica01", "Simin3");
                        natsProcess.Publish("Replica02", "Pimin3");
                        natsProcess.Dispose();
                        Console.WriteLine("End");
            */
            natsProcess.Subscribe("MEReplicationControlPacket", MsgHandler1, false);
            while (true)
            {
                System.Threading.Thread.Sleep(100);
            }


        }

        private static void MsgHandler1(object obj, string message)
        {
            Console.WriteLine($"Getting msg1: {message}");
        }

        private static void MsgHandler2(object obj, string message)
        {
            Console.WriteLine($"Getting msg2: {message}");
        }

    


    }
}
