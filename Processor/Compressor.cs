using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ParallelZipNet.ReadWrite;
using ParallelZipNet.Threading;
using ParallelZipNet.Utils;
using ParallelZipNet.Logger;

namespace ParallelZipNet.Processor {
    public static class Compressor {
        public static void Run(IBinaryReader reader, IBinaryWriter writer, Threading.CancellationToken cancellationToken, Loggers loggers = null) {
            Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(writer, nameof(writer));

            IDefaultLogger defaultLogger = loggers?.DefaultLogger;
            IChunkLogger chunkLogger = loggers?.ChunkLogger;
            IJobLogger jobLogger = loggers?.JobLogger;

            Exception error = null;

            int chunkCount = Convert.ToInt32(reader.Length / Constants.CHUNK_SIZE) + 1;
            writer.WriteInt32(chunkCount);

            int jobNumber = Math.Max(Environment.ProcessorCount - 1, 1);            
            
            var chunks = ReadSource(reader) 
                .AsParallel(jobNumber)
                .Do(x => chunkLogger?.LogChunk("Read", x))
                .Map(ZipChunk)
                .Do(x => chunkLogger?.LogChunk("Proc", x))                
                .AsEnumerable(cancellationToken, x => error = x, jobLogger);
            
            int index = 0;
            foreach(var chunk in chunks) {
                writer.WriteInt32(chunk.Index);
                writer.WriteInt32(chunk.Data.Length);
                writer.WriteBuffer(chunk.Data);                                

                chunkLogger?.LogChunk("Write", chunk);                
                defaultLogger?.LogChunksProcessed(++index, chunkCount);
            }

            if(error != null)
                throw error;
        }

        static IEnumerable<Chunk> ReadSource(IBinaryReader reader) {
            bool isLastChunk;
            int chunkIndex = 0;
            do {                
                long bytesToRead = reader.Length - reader.Position;
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

        static Chunk ZipChunk(Chunk chunk) {
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