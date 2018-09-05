using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Processor2 {
    public class Block {
        readonly Func<object, object> action;
        Block[] inputs;

        public BlockingCollection<object> OutputItems { get; private set; }    

        public Block(Func<object, object> action) {
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
            var result = new Block(null);
            foreach(var block in blocks) {
                block.SetInputs(this);
                result.SetInputs(block);
            }
            return result;
        }

        public Block Pipe(Block block) {
            block.SetInputs(this);
            return block;
        }

        public async Task Run() {
            if(inputs == null) {
                await ReadFromSource();
                return;
            }

            IEnumerable<Task> inputTasks = inputs.Select(input => input.Run());           

            Task task = Task.Run(() => {                
                BlockingCollection<object>[] inputItems = inputs.Select(x => x.OutputItems).ToArray();

                while(!inputItems.All(items => items.IsCompleted)) {
                    int index = BlockingCollection<object>.TakeFromAny(inputItems, out object inputItem);
                    if(index >= 0) {
                        object outputItem = action(inputItem);
                        if(OutputItems != null)
                            OutputItems.Add(outputItem);
                    }
                }
                if(OutputItems != null)
                    OutputItems.CompleteAdding();
            });

            await Task.WhenAll(inputTasks.Concat(new[] { task }));
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
