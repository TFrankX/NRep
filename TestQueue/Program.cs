using System;
using System.Collections.Generic;
using MessageQueue;
using TestQueue;

namespace TestQueue
{
    class Program
    {
        public class Msg
        {
            public long Id;
            public string Mesg;
        }
        static void Main(string[] args)
        {
            MessageQueue.NQueue<Msg> nQueue = new MessageQueue.NQueue<Msg>();
            nQueue.QueueMode = NQueue<Msg>.QueueModes.Queue;
            nQueue.Push(new Msg { Id = 1, Mesg = "A_Message" });
            nQueue.Push(new Msg { Id = 2, Mesg = "B_Message" });
            nQueue.Push(new Msg { Id = 3, Mesg = "C_Message" });

            Console.WriteLine($"MesgQueueCount={nQueue.Count}");
            /*
                        Console.WriteLine($"Preview Obj.Message={nQueue.Pop()?.Mesg}");
                        Console.WriteLine($"Preview Obj.Message={nQueue.Pop()?.Mesg}");
                        Console.WriteLine($"Preview Obj.Message={nQueue.Pop()?.Mesg}");
                        Console.WriteLine($"Preview Obj.Message={nQueue.Pop()?.Mesg}");

            */
            NQueue<Msg>.QueueItem Item;
            Item = nQueue.PopItem();
            Console.WriteLine($"Preview Obj.IdMes={Item?.Id}, Obj.Id={Item?.obj.Id}, Obj.Mesg={Item?.obj.Mesg}");
            Item = nQueue.PopItem();
            Console.WriteLine($"Preview Obj.IdMes={Item?.Id}, Obj.Id={Item?.obj.Id}, Obj.Mesg={Item?.obj.Mesg}");
            Item = nQueue.PopItem();
            Console.WriteLine($"Preview Obj.IdMes={Item?.Id}, Obj.Id={Item?.obj.Id}, Obj.Mesg={Item?.obj.Mesg}");
            Item = nQueue.PopItem();
            Console.WriteLine($"Preview Obj.IdMes={Item?.Id}, Obj.Id={Item?.obj.Id}, Obj.Mesg={Item?.obj.Mesg}");

            SpamGenerator spam = new SpamGenerator();
            Console.WriteLine(spam.RandomString(32));

            // Console.WriteLine($"Preview Obj.ID={nQueue.Preview().Id}, Obj.Message={nQueue.Preview().Mesg}");
        }
    }
}
