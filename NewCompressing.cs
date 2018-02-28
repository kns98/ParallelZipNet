using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System;
using System.Linq;
using System.Collections;
using ParallelZipNet.Threading;
using System.Threading;
using ParallelZipNet.ReadWrite;

namespace ParallelZipNet {
    static class NewCompressing {
        static IEnumerable<Chunk> ReadSource(IBinaryReader reader) {
            bool isLastChunk;
            int chunkIndex = 0;
            do {                
                long bytesToRead = reader.Length - reader.Position;;
                isLastChunk = bytesToRead < Constants.CHUNK_SIZE;
                int readBytes;
                if(isLastChunk) 
                    readBytes = (int)bytesToRead;
                else
                    readBytes = Constants.CHUNK_SIZE;
                byte[] data = reader.ReadBuffer(readBytes);
                yield return new Chunk(chunkIndex++, data);

            }
            while(!isLastChunk);            
            
        }

        static IEnumerable<Chunk> ReadSourceCompressed(IBinaryReader reader, int chunkCount) {
            for(int i = 0; i < chunkCount; i++) {
                int chunkIndex = reader.ReadInt32();
                if(chunkIndex < 0)
                    throw new InvalidDataException("FileCorruptedMessage_1");

                int chunkLength = reader.ReadInt32();
                long bytesToRead = reader.Length - reader.Position;
                if(chunkLength <= 0 || chunkLength > bytesToRead)
                    throw new InvalidDataException("FileCorruptedMessage_2");

                yield return new Chunk(chunkIndex, reader.ReadBuffer(chunkLength));                
            }
        }        

        static Chunk CompressChunk(Chunk chunk) {
            MemoryStream compressed;
            using(compressed = new MemoryStream()) {
                using(var gzip = new GZipStream(compressed, CompressionMode.Compress)) {
                    gzip.Write(chunk.Data, 0, chunk.Data.Length);                    
                    
                }
            }       
            return new Chunk(chunk.Index, compressed.ToArray());            
        }

        static Chunk DecompressChunk(Chunk chunk) {
            MemoryStream compressed;
            MemoryStream decompressed;
            using(compressed = new MemoryStream(chunk.Data)) {
                using(var gzip = new GZipStream(compressed, CompressionMode.Decompress)) {
                    using(decompressed = new MemoryStream()) {
                        gzip.CopyTo(decompressed);                        
                    }
                }
            }
            return new Chunk(chunk.Index, decompressed.ToArray());            
        }

        public static void Log(string action, Chunk chunk) {
            var threadName = Thread.CurrentThread.Name;
            if(string.IsNullOrEmpty(threadName))
                threadName = "thread MAIN";
            Console.WriteLine($"{threadName}:\t{action}\t{chunk?.Index}\t{chunk?.Data.Length}");            
        }      

        public static void Compress(IBinaryReader reader, IBinaryWriter writer, Threading.CancellationToken cancellationToken) {
            int chunkCount = Convert.ToInt32(reader.Length / Constants.CHUNK_SIZE) + 1;
            writer.WriteInt32(chunkCount);

            int jobNumber = Math.Max(Environment.ProcessorCount - 1, 1);            
            
            var chunks = ReadSource(reader) 
                .AsParallel(jobNumber)
                .Do(x => Log("Read", x))
                .Map(CompressChunk)
                .Do(x => Log("Comp", x))                
                .AsEnumerable(cancellationToken, err => Log($"Error Happened: {err.Message}", null));            
            
            foreach(var chunk in chunks) {
                writer.WriteInt32(chunk.Index);
                writer.WriteInt32(chunk.Data.Length);
                writer.WriteBuffer(chunk.Data);                
                Log("Written", chunk);
                // if(chunk.Index > 15) {
                //     Log("CANCEL", null);
                //     cancellationToken.Cancel();
                // }
            }
        }

        public static void Decompress(IBinaryReader reader, IBinaryWriter writer, Threading.CancellationToken cancellationToken) {
            int chunkCount = reader.ReadInt32();
            if(chunkCount <= 0)
                throw new InvalidDataException("FileCorruptedMessage_3");                

            int jobNumber = Math.Max(Environment.ProcessorCount - 1, 1);            

            var chunks = ReadSourceCompressed(reader, chunkCount)
                .AsParallel(jobNumber)
                .Do(x => Log("Read", x))
                .Map(DecompressChunk)
                .Do(x => Log("Decomp", x))
                .AsEnumerable(cancellationToken, err => Log($"Error Happened: {err.Message}", null));

            foreach(var chunk in chunks) {
                long position = chunk.Index * Constants.CHUNK_SIZE;
                writer.WriteBuffer(chunk.Data, position);                
            }
        }
    }
}
