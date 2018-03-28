using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ParallelZipNet.Threading;
using ParallelZipNet.Utils;
using ParallelZipNet.Logger;

namespace ParallelZipNet.Processor {
    public static class Compressor {
        public static void Run(BinaryReader reader, BinaryWriter writer, Threading.CancellationToken cancellationToken, int jobCount,
        Loggers loggers = null) {

            Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(writer, nameof(writer));
            Guard.NotZeroOrNegative(jobCount, nameof(jobCount));

            IDefaultLogger defaultLogger = loggers?.DefaultLogger;
            IChunkLogger chunkLogger = loggers?.ChunkLogger;
            IJobLogger jobLogger = loggers?.JobLogger;

            Exception error = null;

            int chunkCount = Convert.ToInt32(reader.BaseStream.Length / Constants.CHUNK_SIZE) + 1;
            writer.Write(chunkCount);
            
            var chunks = ReadSource(reader) 
                .AsParallel(jobCount)
                .Do(x => chunkLogger?.LogChunk("Read", x))
                .Map(ZipChunk)
                .Do(x => chunkLogger?.LogChunk("Proc", x))                
                .AsEnumerable(cancellationToken, x => error = x, jobLogger);
            
            int index = 0;
            foreach(var chunk in chunks) {
                writer.Write(chunk.Index);
                writer.Write(chunk.Data.Length);
                writer.Write(chunk.Data);                                

                chunkLogger?.LogChunk("Write", chunk);                
                defaultLogger?.LogChunksProcessed(++index, chunkCount);
            }

            if(error != null)
                throw error;
        }

        static IEnumerable<Chunk> ReadSource(BinaryReader reader) {
            bool isLastChunk;
            int chunkIndex = 0;
            do {                
                long bytesToRead = reader.BaseStream.Length - reader.BaseStream.Position;
                isLastChunk = bytesToRead < Constants.CHUNK_SIZE;
                int readBytes;
                if(isLastChunk) 
                    readBytes = (int)bytesToRead;
                else
                    readBytes = Constants.CHUNK_SIZE;
                byte[] data = reader.ReadBytes(readBytes);
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