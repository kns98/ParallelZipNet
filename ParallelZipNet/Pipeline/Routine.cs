using System;
using System.Threading.Tasks;
using ParallelZipNet.Pipeline.Channels;
using ParallelZipNet.Threading;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Pipeline {
    public interface IRoutine {
        Task Run(CancellationToken cancellationToken = null);
    }

    public class Routine<T, U> : IRoutine {
        readonly string name;
        readonly Func<T, U> transform;
        readonly IReadableChannel<T> inputChannel;
        readonly IWritableChannel<U> outputChannel;

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

        public Task Run(CancellationToken cancellationToken) {
            Guard.NotNull(cancellationToken, nameof(cancellationToken));

            return Task.Factory.StartNew(() => {
                try {
                    while(inputChannel.Read(out T data)) {
                        if(cancellationToken.IsCancelled)
                            break;
                        outputChannel.Write(transform(data));                    
                    }
                }
                finally {
                    outputChannel.Finish();
                }
            }, TaskCreationOptions.LongRunning);
        }
    }
}