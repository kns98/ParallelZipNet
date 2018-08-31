using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Processor2 {
    public class TransferStream : Stream {
        Stream writableStream;
        BlockingCollection<byte[]> chunks;
        Task task1, task2;

        public TransferStream(Stream writableStream) {
            Guard.NotNull(writableStream, nameof(writableStream));

            this.writableStream = writableStream;
            this.chunks = new BlockingCollection<byte[]>();
            this.task1 = Task.Factory.StartNew(() => {
                foreach(byte[] chunk in chunks.GetConsumingEnumerable()) {
                    Console.WriteLine($"{Task.CurrentId} End Writing Chunk {chunk.Length}");
                    this.writableStream.Write(chunk, 0, chunk.Length);
                }
            }, TaskCreationOptions.LongRunning);

            this.task2 = Task.Factory.StartNew(() => {
                foreach(byte[] chunk in chunks.GetConsumingEnumerable()) {                    
                    Console.WriteLine($"{Task.CurrentId} End Writing Chunk {chunk.Length}");
                    this.writableStream.Write(chunk, 0, chunk.Length);
                }
            }, TaskCreationOptions.LongRunning);            
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
                Console.WriteLine("Close - Waiting");
                Task.WhenAll(task1, task2).GetAwaiter().GetResult();
                Console.WriteLine("Close - Finish");
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
            Console.WriteLine($"{Task.CurrentId} Start Writing Chunk");
            var chunk = new byte[count];
            Buffer.BlockCopy(buffer, offset, chunk, 0, count);
            chunks.Add(chunk);
        }
    }
}