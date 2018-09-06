using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ParallelZipNet.Processor;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Processor2 {
    public static class Compressor2 {
        public static void Run(BinaryReader reader, BinaryWriter writer) {                        
            // using(var reduce = new TransferStream("reduce", writer.BaseStream))
            // using(var gzip = new GZipStream(reduce, CompressionMode.Compress))
            // // using(var gzip2 = new GZipStream(reduce, CompressionMode.Compress))
            // using(var map = new TransferStream("map", ZipChunk, ZipChunk/*, gzip2*/)) {
            //     reader.BaseStream.CopyTo(map);
            // }

            var readEnumerator = ReadSource(reader, Constants.DEFAULT_CHUNK_SIZE).GetEnumerator();

            var blockReader = new Block("Reader", _ => {
                bool next = readEnumerator.MoveNext();
                if(next) {
                    Console.WriteLine($"{Task.CurrentId} Read Chunk\n");
                    return readEnumerator.Current;
                }
                else
                    return null;
            });
            var blocksZip = Enumerable.Range(0, 6).Select((_, index) => new Block($"Zip {index}", chunk => ZipChunk((Chunk)chunk))).ToArray();
            var blockWriter = new Block("Writer", chunk => {
                Console.WriteLine($"{Task.CurrentId} Write Chunk {((Chunk)chunk).Index}");
                writer.Write(((Chunk)chunk).Index);
                writer.Write(((Chunk)chunk).Data.Length);
                writer.Write(((Chunk)chunk).Data);
                return null;
            });

            var pipeline = blockReader
                .Pipe(blocksZip)
                .Pipe(blockWriter);

            pipeline.Run()
                .GetAwaiter()
                .GetResult();
        }

        static IEnumerable<Chunk> ReadSource(BinaryReader reader, int chunkSize) {
            bool isLastChunk;
            int chunkIndex = 0;
            do {                
                long bytesToRead = reader.BaseStream.Length - reader.BaseStream.Position;
                isLastChunk = bytesToRead < chunkSize;
                int readBytes;
                if(isLastChunk) 
                    readBytes = (int)bytesToRead;
                else
                    readBytes = chunkSize;
                byte[] data = reader.ReadBytes(readBytes);
                yield return new Chunk(chunkIndex++, data);

            }
            while(!isLastChunk);            
        }

        static Chunk ZipChunk(Chunk chunk) {
            Console.WriteLine($"{Task.CurrentId} Zip Chunk {chunk.Index}");
            MemoryStream compressed;
            using(compressed = new MemoryStream()) {
                using(var gzip = new GZipStream(compressed, CompressionMode.Compress)) {
                    gzip.Write(chunk.Data, 0, chunk.Data.Length);                    
                    
                }
            }       
            return new Chunk(chunk.Index, compressed.ToArray());            
        }
    }
}
