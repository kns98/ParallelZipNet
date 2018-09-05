using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ParallelZipNet.Processor;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Processor2 {
    public class TransferStream : Stream {
        BlockingCollection<Chunk> chunks;
        Task[] tasks;
        string name;

        volatile int index = 0;

        public TransferStream(string name, params Func<Chunk, Chunk>[] compressors) {
            Guard.NotNull(compressors, nameof(compressors));

            this.chunks = new BlockingCollection<Chunk>();
            this.name = name;

            tasks = new Task[compressors.Length];

            for(int i = 0; i < compressors.Length; i++) {
                int index = i;
                tasks[index] = Task.Factory.StartNew(() => {
                    foreach(Chunk chunk in chunks.GetConsumingEnumerable()) {
                        Console.WriteLine($"{name} {Task.CurrentId} End Writing Chunk {chunk.Index} {chunk.Data.Length}");
                        compressors[index](chunk);
                        // writableStreams[index].Write(chunk.Data, 0, chunk.Data.Length);
                    }
                }, TaskCreationOptions.LongRunning);
            }
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException(); 
        }

        public override void Flush() {           
            Console.WriteLine("Flush");
        }
 
        public override void Close() {
            chunks.CompleteAdding();
            try {
                Console.WriteLine($"{name} {Task.CurrentId} Close - Waiting");
                Task.WhenAll(tasks).GetAwaiter().GetResult();
                Console.WriteLine($"{name} {Task.CurrentId} Close - Finish");
            }
            finally {                
                base.Close();
            }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotImplementedException();
        }

        public override void SetLength(long value) {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            Interlocked.Increment(ref index);

            Console.WriteLine($"{name} {Task.CurrentId} Start Writing Chunk {index}");
            var buff = new byte[count];
            Buffer.BlockCopy(buffer, offset, buff, 0, count);            
            chunks.Add(new Chunk(index, buff));
        }
    }
}