using System;
using System.Collections.Generic;
using System.Threading;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Threading {
    public class Job<T> : IDisposable where T : class {
        readonly Queue<T> results = new Queue<T>();        
        readonly Thread thread;        
        readonly IEnumerable<T> enumeration;
        readonly CancellationToken cancellationToken;
        public Exception Error { get; private set; }
        public bool IsFinished { 
            get { 
                if(thread.IsAlive)
                    return false;
                lock(results) {
                    return results.Count  == 0;                    
                }
            }
        }        

        public Job(string name, IEnumerable<T> enumeration, CancellationToken cancellationToken) {
            Guard.NotNull(name, nameof(name));
            Guard.NotNull(enumeration, nameof(enumeration));
            Guard.NotNull(cancellationToken, nameof(cancellationToken));

            this.enumeration = enumeration;
            this.cancellationToken = cancellationToken;
            thread = new Thread(Run) {
                Name = name,
                IsBackground = false
            };            
            thread.Start();            
        }

        public T GetResult() {
            lock(results) {
                return results.Count > 0 ? results.Dequeue() : null;
            }
        }             

        public void Dispose() {
            thread.Join();
        }

        void Run() {
            try {           
                foreach(T result in enumeration) {
                    if(cancellationToken.IsCancelled)
                        break;

                    lock(results) {
                        results.Enqueue(result);
                    }
                }
            }
            catch(Exception e) {
                Error = e;
            }
        }        
    }
}