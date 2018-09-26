using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ParallelZipNet.Pipeline.Channels {
    public class Channel<T> : IReadableChannel<T>, IWritableChannel<T> {
        readonly Queue<T> queue = new Queue<T>();
        readonly object locker = new object();

        bool finished = false;

        // readonly BlockingCollection<T> collection = new BlockingCollection<T>(1000);

        // public BlockingCollection<T> UnderlyingCollection => collection;

        public bool Read(out T data) {
            return Read(out data, Timeout.Infinite).Value;
        }
        public bool? Read(out T data, int timeout) {
            data = default(T);

            bool result;

            lock(locker) {
                while(queue.Count == 0 && !finished) {
                    bool gotItem = Monitor.Wait(locker, timeout);
                    if(!gotItem)
                        return null;
                }
                
                result = queue.Count > 0;
                data = queue.Dequeue();                
            }

            return result;

            // if(collection.IsCompleted)
            //     return false;

            // try {
            //     data = collection.Take();                
            // }
            // catch(InvalidOperationException) {
            //     return false;
            // }
            // return true;
        }

        public void Write(T data) {
            lock(locker) {
                queue.Enqueue(data);
                Monitor.Pulse(locker);
            }
            // collection.Add(data);
        }

        public void Finish() {
            lock(locker) {
                finished = true;
                Monitor.PulseAll(locker);
            }
            // collection.CompleteAdding();
        }
    }
}