using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Pipeline.Channels {
    public class Channel<T> : IReadableChannel<T>, IWritableChannel<T> {
        readonly Queue<T> queue = new Queue<T>();
        readonly object locker = new object();
        readonly string name;

        int writerCount;

        public Channel(string name, int writerCount) {
            Guard.NotZeroOrNegative(writerCount, nameof(writerCount));

            this.name = name;
            this.writerCount = writerCount;
        }

        public bool Read(out T data, Profiler profiler = null) {
            data = default(T);

            lock(locker) {
                while(queue.Count == 0 && writerCount > 0)
                    Monitor.Wait(locker);

                profiler?.LogValue(queue.Count, ProfilingType.Channel);
              
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
                writerCount--;
                if(writerCount == 0)
                    Monitor.PulseAll(locker);
            }
        }
    }
}