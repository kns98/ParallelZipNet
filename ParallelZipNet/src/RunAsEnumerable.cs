using System;
using System.IO;
using Guards;
using ParallelContext;
using ParallelCore;
using ParallelZipNet.ChunkLayer;
using ParallelZipNet.Logger;

namespace ParallelZipNet {
    public static class RunAsEnumerable {
        public delegate void Action(BinaryReader reader, BinaryWriter writer, int jobCount, int chunkSize,
            CancellationToken cancellationToken, Loggers loggers);

        public static void Compress(BinaryReader reader, BinaryWriter writer, int jobCount, int chunkSize = Constants.DEFAULT_CHUNK_SIZE,
            CancellationToken cancellationToken = null, Loggers loggers = null) {

            Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(writer, nameof(writer));
            Guard.NotZeroOrNegative(jobCount, nameof(jobCount));
            Guard.NotZeroOrNegative(chunkSize, nameof(chunkSize));

            Console.WriteLine("Compress as Enumerable");

            IDefaultLogger defaultLogger = loggers?.DefaultLogger;
            IChunkLogger chunkLogger = loggers?.ChunkLogger;
            IJobLogger jobLogger = loggers?.JobLogger;            

            ChunkTarget.WriteHeader(reader, writer, chunkSize, out int chunkCount);
            
            var chunks = ChunkSource.ReadChunk(reader, chunkSize) 
                .AsParallel(jobCount)
                .Do(x => chunkLogger?.LogChunk("Read", x))                
                .Map(ChunkConverter.Zip)
                .Do(x => chunkLogger?.LogChunk("Proc", x))                
                .AsEnumerable(cancellationToken, jobLogger);
            
            int index = 0;
            foreach(var chunk in chunks) {
                ChunkTarget.WriteChunkCompressed(chunk, writer);

                chunkLogger?.LogChunk("Write", chunk);                
                defaultLogger?.LogChunksProcessed(++index, chunkCount);
            }
        }

        public static void Decompress(BinaryReader reader, BinaryWriter writer, int jobCount, int chunkSize = Constants.DEFAULT_CHUNK_SIZE,
            CancellationToken cancellationToken = null, Loggers loggers = null) {

            Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(writer, nameof(writer));
            Guard.NotZeroOrNegative(jobCount, nameof(jobCount));
            Guard.NotZeroOrNegative(chunkSize, nameof(chunkSize));

            Console.WriteLine("Decompress as Enumerable");

            IDefaultLogger defaultLogger = loggers?.DefaultLogger;
            IChunkLogger chunkLogger = loggers?.ChunkLogger;
            IJobLogger jobLogger = loggers?.JobLogger;

            ChunkSource.ReadHeader(reader, out int chunkCount);

            var chunks = ChunkSource.ReadChunkCompressed(reader, chunkCount)
                .AsParallel(jobCount)                
                .Do(x => chunkLogger?.LogChunk("Read", x))
                .Map(ChunkConverter.Unzip)
                .Do(x => chunkLogger?.LogChunk("Proc", x))
                .AsEnumerable(cancellationToken, jobLogger);

            int index = 0;
            foreach(var chunk in chunks) {
                ChunkTarget.WriteChunk(chunk, writer, chunkSize);

                chunkLogger?.LogChunk("Write", chunk);
                defaultLogger?.LogChunksProcessed(++index, chunkCount);
            }
        }
    }
}