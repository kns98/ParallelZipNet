using System;
using System.Threading;
using System.Threading.Tasks;
using ParallelZipNet.Pipeline.Channels;
using ParallelZipNet.Threading;
using ParallelZipNet.Utils;

using CancellationToken = ParallelZipNet.Threading.CancellationToken;

namespace ParallelZipNet.Pipeline {
    public interface IRoutine {
        Exception Error { get; }
        
        void Run(CancellationToken cancellationToken = null);
        void Wait();
    }

    public class Routine<T, U> : IRoutine {
        readonly string name;
        readonly Func<T, U> transform;
        readonly IReadableChannel<T> inputChannel;
        readonly IWritableChannel<U> outputChannel;

        Thread thread;

        public Exception Error { get; private set; }

        public Routine(string name, Func<T, U> transform, IReadableChannel<T> inputChannel, IWritableChannel<U> outputChannel) {
            Guard.NotNullOrWhiteSpace(name, nameof(name));
            Guard.NotNull(transform, nameof(transform));
            Guard.NotNull(inputChannel, nameof(inputChannel));
            Guard.NotNull(outputChannel, nameof(outputChannel));

            this.name = name;
            this.transform = transform;
            this.inputChannel = inputChannel;
            this.outputChannel = outputChannel;
        }

        public void Run(CancellationToken cancellationToken) {
            Guard.NotNull(cancellationToken, nameof(cancellationToken));

            if(thread != null)
                throw new InvalidOperationException();

            thread = new Thread(() => {
                try {
                    while(inputChannel.Read(out T data)) {                        
                        if(cancellationToken.IsCancelled)
                            break;                        
                        outputChannel.Write(transform(data));                    
                    }
                }
                catch(Exception e) {
                    Error = e;
                    cancellationToken.Cancel();
                }
                finally {
                    outputChannel.Finish();
                }
            }) {
                Name = name,
                IsBackground = true
            };
            thread.Start();
        }

        public void Wait() {
            thread.Join();
        }
    }
}