using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Processor2 {
    public interface IBlockingChannel<T> {
        BlockingCollection<T> UnderlyingCollection { get; }
    }

    public interface IReadableChannel<T> {
        bool Read(out T data);
    }

    public interface IWritableChannel<T> {
        void Write(T data);
        void Finish();
    }

    public class Channel<T> : IReadableChannel<T>, IBlockingChannel<T>, IWritableChannel<T> {
        readonly BlockingCollection<T> collection = new BlockingCollection<T>();

        BlockingCollection<T> IBlockingChannel<T>.UnderlyingCollection => collection;

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
        readonly BlockingCollection<T>[] collections;

        public CompositeChannel(IEnumerable<IBlockingChannel<T>> channels) {
            collections = channels.Select(channel => channel.UnderlyingCollection).ToArray();
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

    public class Block2<T, U> : IRoutine {
        readonly string name;
        readonly Func<T, U> transform;
        readonly IReadableChannel<T> inputChannel;
        readonly IWritableChannel<U> outputChannel;

        public Block2(string name, Func<T, U> transform, IReadableChannel<T> inputChannel, IWritableChannel<U> outputChannel) {
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
            var block = new Block2<T, U>(name, transform, inputChannel, outputChannel);            
            return new Pipeline<U>(outputChannel, routines.Concat(new[] { block }));
        }

        public Pipeline<U> PipeMany<U>(string name, Func<T, U> transform, int degreeOfParallelism) {
            var results = Enumerable.Range(1, degreeOfParallelism)
                .Select(index => {
                    var outputChannel = new Channel<U>();
                    var block = new Block2<T, U>($"newName {index}", transform, inputChannel, outputChannel);
                    return new {
                        outputChannel,
                        block
                    };
                });

            return new Pipeline<U>(
                new CompositeChannel<U>(results.Select(x => x.outputChannel)),
                routines.Concat(results.Select(x => x.block)));

        }

        public IRoutine Done(string name, Action<T> doneAction) {
            var block = new Block2<T, T>(name, _ => _, inputChannel, new TargetChannel<T>(doneAction));
            return new Pipeline<T>(null, routines.Concat(new[] { block }));
        }

        public Task Run() {
            return Task.WhenAll(routines.Select(routine => routine.Run()));
        }
    }

    public class Block {
        readonly string name;
        readonly Func<object, object> action;
        Block[] inputs;
        bool isStarted = false;

        public BlockingCollection<object> OutputItems { get; private set; }    

        public Block(string name, Func<object, object> action) {
            this.name = name;
            this.action = action;
        }

        void SetInputs(params Block[] inputs) {
            this.inputs = inputs;
            foreach(var input in inputs)
                input.InitOutput();
        }

        void InitOutput() {
            OutputItems = new BlockingCollection<object>();
        }

        public Block Pipe(Block[] blocks) {
            var result = new Block("Dummy", null);
            foreach(var block in blocks)
                block.SetInputs(this);                
            result.SetInputs(blocks);
            return result;
        }

        public Block Pipe(Block block) {
            block.SetInputs(this);
            return block;
        }

        public Task Run() {
            isStarted = true;

            if(inputs == null) {
                return ReadFromSource();
            }

            Task[] inputTasks = inputs.Where(input => !input.isStarted).Select(input => input.Run()).ToArray();           

            Task task = Task.Run(() => {                
                BlockingCollection<object>[] inputItems = inputs.Select(x => x.OutputItems).ToArray();

                while(!inputItems.All(items => items.IsCompleted)) {
                    object inputItem = null;
                    try {
                        BlockingCollection<object>.TakeFromAny(inputItems, out inputItem);
                    }
                    catch(ArgumentException) {
                        Console.WriteLine($"{name} FINISHED");
                        break;
                    }
                    object outputItem = action != null ? action(inputItem) : inputItem;
                    if(OutputItems != null)
                        OutputItems.Add(outputItem);
                }
                if(OutputItems != null)
                    OutputItems.CompleteAdding();
            });

            return Task.WhenAll(inputTasks.Concat(new[] { task }));
        }

        Task ReadFromSource() {
            return Task.Run(() => {
                object item;
                while((item = action(null)) != null) {
                    OutputItems.Add(item);                    
                }
                OutputItems.CompleteAdding();
            });
        }
    }
}
