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
        void Run(CancellationToken cancellationToken, ProfilePipeline profile);
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

        public void Run(CancellationToken cancellationToken, ProfilePipeline profile) {
            Guard.NotNull(cancellationToken, nameof(cancellationToken));

            if(thread != null)
                throw new InvalidOperationException();

            thread = new Thread(() => {
                try {
                    if(profile == ProfilePipeline.None)
                        Do(cancellationToken);                        
                    else
                        DoAndProfile(cancellationToken, profile);                        
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

        void Do(CancellationToken cancellationToken) {
            while(inputChannel.Read(out T data)) {                        
                if(cancellationToken.IsCancelled)
                    break;                        
                outputChannel.Write(transform(data));                    
            }                        
        }
        
        void DoAndProfile(CancellationToken cancellationToken, ProfilePipeline profile) {
            bool showReadTime = profile.HasFlag(ProfilePipeline.Read);
            bool showWriteTime = profile.HasFlag(ProfilePipeline.Write);
            bool showTransformTime = profile.HasFlag(ProfilePipeline.Transform);

            Stopwatch watch = new Stopwatch();

            void BeginWatch(bool force) {
                if(force) {
                    watch.Reset();
                    watch.Start();
                }
            }

            void EndWatch(bool force, string operation) {
                if(force) {
                    watch.Stop();            
                    double timeMicro = (double)watch.ElapsedTicks / Stopwatch.Frequency * 1000 * 1000;
                    Console.WriteLine($"{operation} - {name} : {timeMicro}");
                }
            }            

            bool Read(out T data) {                
                BeginWatch(showReadTime);
                bool finished = inputChannel.Read(out data);
                EndWatch(showReadTime, "READ");
                return finished;
            }

            U Transform(T data) {
                BeginWatch(showTransformTime);
                U result = transform(data);
                EndWatch(showTransformTime, "TRANS");
                return result;
            }

            void Write(U data) {
                BeginWatch(showWriteTime);
                outputChannel.Write(data);
                EndWatch(showWriteTime, "WRITE");
            }            

            while(Read(out T data)) {                        
                if(cancellationToken.IsCancelled)
                    break;                        
                Write(Transform(data));                    
            }                        
        }
    }
}