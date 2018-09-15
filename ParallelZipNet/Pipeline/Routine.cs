using System;
using System.Threading.Tasks;
using ParallelZipNet.Pipeline.Channels;

namespace ParallelZipNet.Pipeline {
    public interface IRoutine {
        Task Run();
    }

    public class Routine<T, U> : IRoutine {
        readonly string name;
        readonly Func<T, U> transform;
        readonly IReadableChannel<T> inputChannel;
        readonly IWritableChannel<U> outputChannel;

        public Routine(string name, Func<T, U> transform, IReadableChannel<T> inputChannel, IWritableChannel<U> outputChannel) {
            this.name = name;
            this.transform = transform;
            this.inputChannel = inputChannel;
            this.outputChannel = outputChannel;
        }

        public Task Run() {
            return Task.Run(() => {                
                while(inputChannel.Read(out T data))
                    outputChannel.Write(transform(data));
                outputChannel.Finish();
            });
        }
    }
}