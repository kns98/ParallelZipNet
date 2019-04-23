using System;
using System.IO;
using Guards;
using ParallelCore;
using ParallelPipeline;
using ParallelZipNet.ChunkLayer;
using ParallelZipNet.Logger;

namespace ParallelZipNet {
    public static class RunAsPipeline {
        public delegate void Action(BinaryReader reader, BinaryWriter writer, int jobCount, int chunkSize,
            CancellationToken cancellationToken, Loggers loggers, ProfilingType profilingType);

        public static void Compress(BinaryReader reader, BinaryWriter writer, int jobCount, int chunkSize = Constants.DEFAULT_CHUNK_SIZE,
            CancellationToken cancellationToken = null, Loggers loggers = null, ProfilingType profilingType = ProfilingType.None) {

            Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(writer, nameof(writer));
            Guard.NotZeroOrNegative(jobCount, nameof(jobCount));
            Guard.NotZeroOrNegative(chunkSize, nameof(chunkSize));

            Console.WriteLine("Compress as Pipeline");

            IDefaultLogger defaultLogger = loggers?.DefaultLogger;
            int index = 0; 

            ChunkTarget.WriteHeader(reader, writer, chunkSize, out int chunkCount);

            var chunkEnumerator = ChunkSource.ReadChunk(reader, chunkSize).GetEnumerator();
            
            Pipeline<Chunk>
                .FromSource("source", (out Chunk chunk) => {
                    bool next = chunkEnumerator.MoveNext();
                    chunk = next ? chunkEnumerator.Current : null;
                    return next;
                })
                .PipeMany("zip", ChunkConverter.Zip, jobCount)
                .ToTarget("target", (Chunk chunk) => {
                    ChunkTarget.WriteChunkCompressed(chunk, writer);

                    if(profilingType == ProfilingType.None)
                        defaultLogger?.LogChunksProcessed(++index, chunkCount);                    
                })
                .Run(cancellationToken, profilingType);
        }

         public static void Decompress(BinaryReader reader, BinaryWriter writer, int jobCount, int chunkSize = Constants.DEFAULT_CHUNK_SIZE,
            CancellationToken cancellationToken = null, Loggers loggers = null, ProfilingType profilingType = ProfilingType.None) {                        

            Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(writer, nameof(writer));
            Guard.NotZeroOrNegative(jobCount, nameof(jobCount));
            Guard.NotZeroOrNegative(chunkSize, nameof(chunkSize));

            Console.WriteLine("Decompress as Pipeline");

            int index = 0;

            IDefaultLogger defaultLogger = loggers?.DefaultLogger;                        

            ChunkSource.ReadHeader(reader, out int chunkCount);

            var chunkEnumerator = ChunkSource.ReadChunkCompressed(reader, chunkCount).GetEnumerator();

            Pipeline<Chunk>
                .FromSource("source", (out Chunk chunk) => {
                    bool next = chunkEnumerator.MoveNext();
                    chunk = next ? chunkEnumerator.Current : null;
                    return next;
                })
                .PipeMany("zip", ChunkConverter.Unzip, jobCount)
                .ToTarget("target", (Chunk chunk) => {
                    ChunkTarget.WriteChunk(chunk, writer, chunkSize);

                    if(profilingType == ProfilingType.None)
                        defaultLogger?.LogChunksProcessed(++index, chunkCount);
                })
                .Run(cancellationToken, profilingType);
        }  
    }
}