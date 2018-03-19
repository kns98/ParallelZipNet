using System;
using System.Collections.Generic;
using System.Threading;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Threading {
    public class Job<T> : IDisposable where T : class {
        const int timeout = 10;

        readonly Queue<T> results = new Queue<T>();        
        readonly Thread thread;        
        readonly IEnumerable<T> enumeration;
        readonly CancellationToken cancellationToken;
        readonly ReaderWriterLock resultsLock = new ReaderWriterLock();

        public Exception Error { get; private set; }
        public bool IsFinished { 
            get { 
                if(thread.IsAlive)
                    return false;
                return ResultsCount == 0;
            }
        }
        public int ResultsCount {
            get {
                int count = 0;
                SafeRead(() => count = results.Count);
                return count;
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
                IsBackground = true
            };            
            thread.Start();            
        }

        public T GetResult() {
            T result = null;
            SafeWrite(() => {
                if(results.Count > 0)
                    result = results.Dequeue();
            });
            return result;
        }             

        public void Dispose() {
            thread.Join();
        }

        void SafeRead(Action action) {
            try {
                resultsLock.AcquireReaderLock(timeout);
                action();
            }
            finally {
                resultsLock.ReleaseReaderLock();                    
            }                        
        }

        void SafeWrite(Action action) {
            resultsLock.AcquireWriterLock(timeout);
            try {                
                action();
            }
            finally {
                resultsLock.ReleaseWriterLock();
            }
        }

        void Run() {
            try {           
                foreach(T result in enumeration) {
                    if(cancellationToken.IsCancelled)
                        break;

                    SafeWrite(() => results.Enqueue(result));
                }
            }
            catch(Exception e) {
                Error = e;
            }
        }        
    }
}