using System;
using System.Threading;
using Newtonsoft.Json;
using NLog;


namespace MessageQueue
{
    using System.Collections.Generic;

    /// <summary>
    /// Queue class
    /// </summary>
    /// <typeparam name="T"> type object to put in queue </typeparam>
    public class NQueue<T> where T : class
    {
       
        

        
        
        /// <summary>
        /// Main collection of class
        /// </summary>
        public readonly LinkedList<QueueItem> items = new LinkedList<QueueItem>();

        /// <summary>
        /// Store for last generating ID
        /// </summary>
        private static long _lastId = DateTime.UtcNow.Ticks;

        public enum QueueModes
        {
            /// <summary>
            /// queue mode: new object will be returned last.
            /// </summary>
            Queue,
            /// <summary>
            /// stack mode: new object will be returned first.
            /// </summary>
            Stack,
        }
        /// <summary>
        /// return number objects in queue
        /// </summary>
        public int Count => items.Count;

        /// <summary>
        /// return queue mode 
        /// default mode: <value>Queue</value>.
        /// </summary>
        public long LastPushId
        {
            get { return lastPushedId; }
        }

        /// <summary>
        /// return queue mode 
        /// default mode: <value>Queue</value>.
        /// </summary>
        public QueueModes QueueMode { get; set; }

        /// <summary>
        /// Automatic assign id for adding message
        /// </summary>
        public bool IdAutoAssign { set; get; }
        /// <summary>
        /// Is queue empty
        /// </summary>
        public bool IsEmpty => (items.Count!=0);

        /// <summary>
        /// Delegate for event handler
        /// </summary>
        /// <param name="sender"></param>
        public delegate void AddHandler(object sender,long num);

        public event AddHandler OnAddMessages;

        /// <summary>
        /// Method GetEnumerator for foreach usable
        /// </summary>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(items);
        }
        /// <summary>
        /// Class GetEnumerator for foreach usable
        /// </summary>
        public class Enumerator
        {
            public Enumerator(LinkedList<QueueItem> items)
            {
                itemsList = items;
            }
            private readonly LinkedList<QueueItem> itemsList;
            public bool MoveNext()
            {
                return itemsList.GetEnumerator().MoveNext();
            }
            public object Current => itemsList.GetEnumerator().Current;
        }
        
        /// <summary>
        /// Sort queue by messages id
        /// </summary>
        public void SortABC()
        {
            SortComparerId comparer = new SortComparerId();
            QuickSort(items, comparer);
        }

        private long lastPushedId;   

        /// <summary>
        /// Sort elements in list.
        /// </summary>
        private void QuickSort(LinkedList<QueueItem> linkedList, IComparer<QueueItem> comparer)
        {
            if (linkedList == null || linkedList.Count <= 1) return; // there is nothing to sort
            SortImpl(linkedList.First, linkedList.Last, comparer);
        }
        /// <summary>
        /// Comparer for sort void
        /// </summary>
        private class SortComparerId : IComparer<QueueItem>
        {
            public int Compare(QueueItem x, QueueItem y)
            {
                if (x?.IdNum > y?.IdNum)
                    return 1;
                else if (y?.IdNum < x?.IdNum)
                    return -1;
                else
                    return 0;
            }
        }

        private static void SortImpl(LinkedListNode<QueueItem> head, LinkedListNode<QueueItem> tail, IComparer<QueueItem> comparer)
        {
            if (head == tail) return; // there is nothing to sort

            void Swap(LinkedListNode<QueueItem> a, LinkedListNode<QueueItem> b)
            {
                var tmp = a.Value;
                a.Value = b.Value;
                b.Value = tmp;
            }

            var pivot = tail;
            var node = head;
            while (node?.Next != pivot)
            {
                if (comparer.Compare(node?.Value, pivot?.Value) > 0)
                {
                    Swap(pivot, pivot?.Previous);
                    Swap(node, pivot);
                    pivot = pivot?.Previous; // move pivot backward
                }
                else node = node?.Next; // move node forward
            }
            if (comparer.Compare(node?.Value, pivot?.Value) > 0)
            {
                Swap(node, pivot);
                pivot = node;
            }

            // pivot location is found, now sort nodes below and above pivot.
            // if head or tail is equal to pivot we reached boundaries and we should stop recursion.
            if (head != pivot) SortImpl(head, pivot?.Previous, comparer);
            if (tail != pivot) SortImpl(pivot?.Next, tail, comparer);
        }

        /*
                /// <summary>
                /// queue element.
                /// </summary>
                public  class QueueItem : IEquatable<QueueItem>
                {
                    private readonly object obj;
                    public QueueItem(object Obj)
                    {
                        obj = Obj;
                    }


                    public bool Equals(QueueItem other)
                    {
                        return obj == other?.obj;
                    }
                }
        */

        /// <summary>
        /// Queue element.
        /// </summary>
        public class QueueItem 
        {
            /// <summary>
            /// Type of object to store in queue element.
            /// </summary>
            public readonly T obj;
            /// <summary>
            /// String Id of new element.
            /// </summary>
            public readonly string Id;
            /// <summary>
            /// Numeric Id of new element.
            /// </summary>
            public long IdNum;
            /// <summary>
            /// Element Id is automatic assigned.
            /// </summary>
            public bool IdAutoAssign { get; }

            public QueueItem(T Obj)
            {
                obj = Obj;
                IdAutoAssign = true;
                if (!IdAutoAssign) return;
                Id = GetNextId();
                IdNum = _lastId;
            }
            
            public QueueItem(T Obj,long idNum)
            {
                obj = Obj;
                IdAutoAssign = true;
                if (!IdAutoAssign) return;
                Id = GetNextId();
                IdNum = idNum;
            }
            /// <summary>
            /// Fake constructor for Json deserialize.
            /// </summary>
            [JsonConstructor]
            public QueueItem(T Obj,bool fake)
            {
                obj = Obj;
            }

            /// <summary>
            /// Id generator for automatic assign to messages.
            /// </summary>
            private const string _encode32Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV";
            private string GetNextId() => GenerateId(Interlocked.Increment(ref _lastId));
            private string GenerateId(long id)
            {
                {
                    var charBuffer = new char[13];
                    charBuffer[0] = _encode32Chars[(int)(id >> 60) & 31];
                    charBuffer[1] = _encode32Chars[(int)(id >> 55) & 31];
                    charBuffer[2] = _encode32Chars[(int)(id >> 50) & 31];
                    charBuffer[3] = _encode32Chars[(int)(id >> 45) & 31];
                    charBuffer[4] = _encode32Chars[(int)(id >> 40) & 31];
                    charBuffer[5] = _encode32Chars[(int)(id >> 35) & 31];
                    charBuffer[6] = _encode32Chars[(int)(id >> 30) & 31];
                    charBuffer[7] = _encode32Chars[(int)(id >> 25) & 31];
                    charBuffer[8] = _encode32Chars[(int)(id >> 20) & 31];
                    charBuffer[9] = _encode32Chars[(int)(id >> 15) & 31];
                    charBuffer[10] = _encode32Chars[(int)(id >> 10) & 31];
                    charBuffer[11] = _encode32Chars[(int)(id >> 5) & 31];
                    charBuffer[12] = _encode32Chars[(int)id & 31];
                    return new string(charBuffer, 0, 13);
                }
            }
        }


        /// <summary>
        /// Add new object in queue.
        /// </summary>
        /// <param name="Obj"></param>
        public void Push(T Obj)
        {
            PushItem(new QueueItem(Obj));
        }


        /// <summary>
        /// Add new objects in queue.
        /// </summary>
        /// <param name="objects"></param>
        public void Push(List<T> objects)
        {
            List<QueueItem> listItems = new List<QueueItem>();
            foreach (var line in objects)
            {
                listItems.Add(new QueueItem(line));
            }
            PushItem(listItems);
        }

        /// <summary>
        /// Add new object in queue with custom id
        /// </summary>
        /// <param name="Obj"></param>
        /// <param name="idNum"></param>
        public void Push(T Obj,long idNum)
        {
            PushItem(new QueueItem(Obj, idNum));
        }
        /// <summary>
        /// Get an object from queue
        /// </summary>
        public T Pop()
        {
            QueueItem queueItem;
           

            if (items.Count>0)
            { 
                switch (QueueMode)
                {
                    case QueueModes.Queue:
                        queueItem = items.First.Value;
                        items.RemoveFirst();
                        break;
                    case QueueModes.Stack:
                        queueItem = items.First.Value;
                        items.RemoveFirst();
                        break;
                    default:
                        return null;
                }
                
            }
            else return null;
            return queueItem.obj;
        }
        /// <summary>
        /// Get an item (user object with additional attributes) from queue
        /// </summary>
        public QueueItem PopItem()
        {
            QueueItem queueItem;

            if (items.Count > 0)
            {
                switch (QueueMode)
                {
                    case QueueModes.Queue:
                        queueItem = items.First.Value;
                        items.RemoveFirst();
                        break;
                    case QueueModes.Stack:
                        queueItem = items.First.Value;
                        items.RemoveFirst();
                        break;
                    default:
                        return null;
                }

            }
            else return null;
            return queueItem;
        }
        /// <summary>
        /// Preview current object in queue without deleting
        /// </summary>
        public T PreviewCurrent()
        {
            QueueItem queueItem;
            if (items.Count > 0)
            {
                switch (QueueMode)
                {
                    case QueueModes.Queue:
                        queueItem = items.First.Value;
                        break;
                    case QueueModes.Stack:
                        queueItem = items.Last.Value;
                        break;
                    default:
                        return null;
                }
            }
            else return null;
            return queueItem.obj;
        }
        /// <summary>
        /// Preview current item (user object with additional attributes) in queue without deleting
        /// </summary>
        public QueueItem PreviewCurrentItem()
        {
            QueueItem queueItem;
            if (items.Count > 0)
            {
                switch (QueueMode)
                {
                    case QueueModes.Queue:
                        queueItem = items.First.Value;
                        break;
                    case QueueModes.Stack:
                        queueItem = items.Last.Value;
                        break;
                    default:
                        return null;
                }
            }
            else return null;
            return queueItem;
        }

        /// <summary>
        /// add the new object in queue.
        /// </summary>
        /// <param name="NewItem"></param>
        public void PushItem(QueueItem NewItem)
        {
            //if (items.Contains(NewItem))
            //  return;
            //      OnAddMessages?.Invoke(this, 1);



            switch (QueueMode)
            {
                case QueueModes.Queue:
                    items.AddLast(NewItem);
                    break;
                case QueueModes.Stack:
                    items.AddFirst(NewItem);
                    break;
            }
            lastPushedId = NewItem.IdNum;
            OnAddMessages?.Invoke(this, 1);

        }


        /// <summary>
        /// add the new object in queue.
        /// </summary>
        /// <param name="newItems"></param>
        public void PushItem(List<QueueItem> newItems)
        {
            //LinkedList<QueueItem> ListOfObjects = new LinkedList<YourObjectType>(YourObjectArray);
            //if (items.Contains(NewItem))
            //  return;

            switch (QueueMode)
            {
                case QueueModes.Queue:
                    foreach (var line in newItems)
                    {
                        items.AddLast(line);
                    }
                    break;
                case QueueModes.Stack:
                    foreach (var line in newItems)
                    {
                        items.AddFirst(line);
                    }
                    break;
            }
            OnAddMessages?.Invoke(this, newItems.Count);
        }
    }
}
