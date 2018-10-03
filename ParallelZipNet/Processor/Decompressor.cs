using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ParallelZipNet.Threading;
using ParallelZipNet.Utils;
using ParallelZipNet.Logger;
using ParallelZipNet.ChunkLayer;
using ParallelZipNet.Pipeline;

namespace ParallelZipNet.Processor {
    public static class Decompressor {
        public static void RunAsEnumerable(BinaryReader reader, BinaryWriter writer, int jobCount, int chunkSize = Constants.DEFAULT_CHUNK_SIZE,
            Threading.CancellationToken cancellationToken = null, Loggers loggers = null) {

            Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(writer, nameof(writer));
            Guard.NotZeroOrNegative(jobCount, nameof(jobCount));
            Guard.NotZeroOrNegative(chunkSize, nameof(chunkSize));

            Console.WriteLine("Decompress as Enumerable");

            IDefaultLogger defaultLogger = loggers?.DefaultLogger;
            IChunkLogger chunkLogger = loggers?.ChunkLogger;
            IJobLogger jobLogger = loggers?.JobLogger;

            ReadHeader(reader, out int chunkCount);

            var chunks = ChunkSource.ReadCompressed(reader, chunkCount)
                .AsParallel(jobCount)                
                .Do(x => chunkLogger?.LogChunk("Read", x))
                .Map(ChunkConverter.Unzip)
                .Do(x => chunkLogger?.LogChunk("Proc", x))
                .AsEnumerable(cancellationToken, jobLogger);

            int index = 0;
            foreach(var chunk in chunks) {
                ChunkTarget.Write(chunk, writer, chunkSize);

                chunkLogger?.LogChunk("Write", chunk);
                defaultLogger?.LogChunksProcessed(++index, chunkCount);
            }
        }

        public static void RunAsPipeline(BinaryReader reader, BinaryWriter writer, int jobCount, int chunkSize = Constants.DEFAULT_CHUNK_SIZE,
            Threading.CancellationToken cancellationToken = null, Loggers loggers = null) {                        

            Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(writer, nameof(writer));
            Guard.NotZeroOrNegative(jobCount, nameof(jobCount));
            Guard.NotZeroOrNegative(chunkSize, nameof(chunkSize));

            Console.WriteLine("Decompress as Pipeline");

            IDefaultLogger defaultLogger = loggers?.DefaultLogger;
            Action<Chunk> write = ChunkTarget.WriteAction(writer, chunkSize);
            int index = 0;

            ReadHeader(reader, out int chunkCount);

            Pipeline<Chunk>
                .FromSource("read", ChunkSource.ReadCompressedAction(reader, chunkCount))
                .PipeMany("zip", ChunkConverter.Unzip, jobCount)
                .ToTarget("write", (Chunk chunk) => {
                    write(chunk);

                    defaultLogger?.LogChunksProcessed(++index, chunkCount);
                })
                .Run(cancellationToken);
        }  

        static void ReadHeader(BinaryReader reader, out int chunkCount) {
            chunkCount = reader.ReadInt32();
        }
    }
}