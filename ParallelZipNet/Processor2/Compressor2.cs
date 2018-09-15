using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ParallelZipNet.Pipeline;
using ParallelZipNet.Processor;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Processor2 {
    public static class Compressor2 {
        public static void Run(BinaryReader reader, BinaryWriter writer) {
            var readEnumerator = ReadSource(reader, Constants.DEFAULT_CHUNK_SIZE).GetEnumerator();

            Pipeline<Chunk>.FromSource("read", (out Chunk chunk) => {
                bool next = readEnumerator.MoveNext();
                chunk = next ? readEnumerator.Current : null;
                // Console.WriteLine($"Read {next}");
                return next;
            })
            .PipeMany("zip", ZipChunk, Constants.DEFAULT_JOB_COUNT)
            .Done("write", (Chunk chunk) => {
                // Console.WriteLine($"{Task.CurrentId} Write Chunk {chunk.Index}");
                writer.Write(chunk.Index);
                writer.Write(chunk.Data.Length);
                writer.Write(chunk.Data);
            })
            .Run();
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
            // Console.WriteLine($"{Task.CurrentId} Zip Chunk {chunk.Index}");
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
