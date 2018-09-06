using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Processor2 {
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
