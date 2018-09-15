using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Processor2 {
    public interface IReadableChannel<T> {
        bool Read(out T data);
    }

    public interface IWritableChannel<T> {
        void Write(T data);
        void Finish();
    }

    public class Channel<T> : IReadableChannel<T>, IWritableChannel<T> {
        readonly BlockingCollection<T> collection = new BlockingCollection<T>(1000);

        public BlockingCollection<T> UnderlyingCollection => collection;

        public bool Read(out T data) {
            data = default(T);

            if(collection.IsCompleted)
                return false;

            try {
                data = collection.Take();                
            }
            catch(InvalidOperationException) {
                return false;
            }
            return true;
        }

        public void Write(T data) {
            collection.Add(data);
        }

        public void Finish() {
            collection.CompleteAdding();
        }
    }

    public class CompositeChannel<T> : IReadableChannel<T> {
        readonly Channel<T>[] channels;
        readonly BlockingCollection<T>[] collections;

        public Channel<T>[] Channels => channels;

        public CompositeChannel(int degreeOfParallelism) {
            channels = new Channel<T>[degreeOfParallelism];
            collections = new BlockingCollection<T>[degreeOfParallelism];

            for(int i = 0; i < degreeOfParallelism; i++) {
                var channel = new Channel<T>();
                channels[i] = channel;
                collections[i] = channel.UnderlyingCollection;
            }
        }

        public bool Read(out T data) {
            data = default(T);

            if(collections.All(collection => collection.IsCompleted))
                return false;

            try {
                BlockingCollection<T>.TakeFromAny(collections, out data);
            }
            catch(ArgumentException) {
                return false;
            }
            return true;
        }
    }

    public delegate bool SourceAction<T>(out T data);

    public class SourceChannel<T> : IReadableChannel<T> {
        readonly SourceAction<T> action;

        public SourceChannel(SourceAction<T> action) {
            this.action = action;
        }

        public bool Read(out T data) {
            return action(out data);
        }
    }

    public class TargetChannel<T> : IWritableChannel<T> {
        readonly Action<T> action;

        public  TargetChannel(Action<T> action) {
            this.action = action;
        }
        public void Write(T data) {
            action(data);
        }
        public void Finish() {
        }
    }

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

    public class Pipeline<T> : IRoutine {
        public static Pipeline<T> FromSource(string name, SourceAction<T> source) {
            var pipeline = new Pipeline<T>(new SourceChannel<T>(source), new IRoutine[0]);
            return pipeline.Pipe(name, _ => _);
        }

        readonly IEnumerable<IRoutine> routines;        
        readonly IReadableChannel<T> inputChannel;

        Pipeline(IReadableChannel<T> inputChannel, IEnumerable<IRoutine> routines) {
            this.inputChannel = inputChannel;
            this.routines = routines;
        }

        public Pipeline<U> Pipe<U>(string name, Func<T, U> transform) {            
            var outputChannel = new Channel<U>();
            var routine = new Routine<T, U>(name, transform, inputChannel, outputChannel);            
            return new Pipeline<U>(outputChannel, this.routines.Concat(new[] { routine }));
        }
 
        public Pipeline<U> PipeMany<U>(string name, Func<T, U> transform, int degreeOfParallelism) {
            var outputChannel = new CompositeChannel<U>(degreeOfParallelism);
            var routines = outputChannel.Channels
                .Select((channel, index) => new Routine<T, U>($"{name} {index}", transform, inputChannel, channel))
                .ToArray();

            return new Pipeline<U>(outputChannel, this.routines.Concat(routines));
        }

        public IRoutine Done(string name, Action<T> doneAction) {
            var block = new Routine<T, T>(name, _ => _, inputChannel, new TargetChannel<T>(doneAction));
            return new Pipeline<T>(null, routines.Concat(new[] { block }));
        }

        public Task Run() {
            return Task.WhenAll(routines.Select(routine => routine.Run()));
        }
    }
}
  