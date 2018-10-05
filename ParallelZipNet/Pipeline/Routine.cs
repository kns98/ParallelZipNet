using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ParallelZipNet.Pipeline.Channels;
using ParallelZipNet.Threading;
using ParallelZipNet.Utils;

using CancellationToken = ParallelZipNet.Threading.CancellationToken;

namespace ParallelZipNet.Pipeline {
    public interface IRoutine {
        void Run(CancellationToken cancellationToken, ProfilingType profilingType);
        Exception Wait();
    }

    public class Routine<T, U> : IRoutine {
        readonly string name;
        readonly Func<T, U> transform;
        readonly IReadableChannel<T> inputChannel;
        readonly IWritableChannel<U> outputChannel;        

        Thread thread;
        Exception error;

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

        public void Run(CancellationToken cancellationToken, ProfilingType profilingType) {
            Guard.NotNull(cancellationToken, nameof(cancellationToken));

            if(thread != null)
                throw new InvalidOperationException();

            thread = new Thread(() => {
                try {
                    Profiler profiler =
                        profilingType != ProfilingType.None ?
                        new Profiler(name, profilingType) :
                        null;
                    Do(cancellationToken, profiler);
                }
                catch(Exception error) {
                    this.error = error;
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

        public Exception Wait() {
            thread.Join();
            return error;
        }
                
        void Do(CancellationToken cancellationToken, Profiler profiler) {
            bool Read(out T data) {                
                bool finished = true;
                profiler?.BeginWatch(ProfilingType.Read);
                try {
                    finished = inputChannel.Read(out data);
                }
                finally {
                    profiler?.EndWatch(ProfilingType.Read);
                }
                return finished;
            }

            U Transform(T data) {
                U result = default(U);
                profiler?.BeginWatch(ProfilingType.Transform);
                try {
                    result = transform(data);
                }
                finally {
                    profiler?.EndWatch(ProfilingType.Transform);
                }
                return result;
            }

            void Write(U data) {
                profiler?.BeginWatch(ProfilingType.Write);
                try {
                    outputChannel.Write(data);
                }
                finally {
                    profiler?.EndWatch(ProfilingType.Write);
                }
            }            

            while(Read(out T data)) {                        
                if(cancellationToken.IsCancelled)
                    break;                        
                Write(Transform(data));                    
            }                        
        }
    }
}