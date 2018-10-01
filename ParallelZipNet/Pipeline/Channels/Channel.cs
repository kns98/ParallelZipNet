using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ParallelZipNet.Pipeline.Channels {
    public class Channel<T> : IReadableChannel<T>, IWritableChannel<T> {
        readonly Queue<T> queue = new Queue<T>();
        readonly object locker = new object();
        readonly string name;

        bool finished = false;

        public Channel(string name) {
            this.name = name;
        }

        public bool Read(out T data) {
            data = default(T);

            lock(locker) {
                while(queue.Count == 0 && !finished)
                    Monitor.Wait(locker);
              
                if(queue.Count > 0) {
                    data = queue.Dequeue();
                    return true;
                }

                return false;                
            }
        }

        public void Write(T data) {
            lock(locker) {
                queue.Enqueue(data);
                
                Monitor.Pulse(locker);
            }
        }

        public void Finish() {
            lock(locker) {
                finished = true;

                Monitor.PulseAll(locker);
            }
        }
    }
}