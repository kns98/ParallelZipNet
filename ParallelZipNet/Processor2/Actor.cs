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

    public class Block2<T, U> {
        readonly string name;
        readonly Func<T, U> transform;
        IReadableChannel<T> inputChannel;
        IWritableChannel<U> outputChannel;

        public Block2(string name, Func<T, U> transform, IReadableChannel<T> inputChannel, IWritableChannel<U> outputChannel) {
            this.name = name;
            this.transform = transform;
            this.inputChannel = inputChannel;
            this.outputChannel = outputChannel;
        }

        public Block2<U, K> Pipe<K>(Block2<U, K> block) {
            var channel = new Channel<U>();

            InitOutputChannel(channel);
            block.InitInputChannel(channel);

            return block;
        }

        public CompositeBlock2<U, K> PipeMany<K>(params Block2<U, K>[] blocks) {
            var channel = new Channel<U>();

            InitOutputChannel(channel);
            foreach(var block in blocks)
                block.InitInputChannel(channel);

            return new CompositeBlock2<U, K>(blocks);
        }

        public Task Run() {
            return Task.Run(() => {                
                while(inputChannel.Read(out T data))
                    outputChannel.Write(transform(data));
                outputChannel.Finish();
            });
        }

        internal void InitInputChannel(IReadableChannel<T> inputChannel) {
            if(this.inputChannel != null)
                throw new InvalidOperationException();

            this.inputChannel = inputChannel;
        }

        internal void InitOutputChannel(IWritableChannel<U> outputChannel) {
            if(this.outputChannel != null)
                throw new InvalidOperationException();

            this.outputChannel = outputChannel;
        }
    }

    public class CompositeBlock2<T, U> {
        readonly IEnumerable<Block2<T, U>> blocks;

        public CompositeBlock2(IEnumerable<Block2<T, U>> blocks) {
            this.blocks = blocks;
        }

        public Block2<U, K> Pipe<K>(Block2<U, K> block) {
            var channels = new List<Channel<U>>();
            foreach(var item in blocks) {
                var channel = new Channel<U>();
                item.InitOutputChannel(channel);
                channels.Add(channel);
            }

            var composite = new CompositeChannel<U>(channels);
            block.InitInputChannel(composite);
            return block;
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
