using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ParallelZipNet.Threading;
using ParallelZipNet.Utils;
using ParallelZipNet.Logger;
using ParallelZipNet.ChunkLayer;

namespace ParallelZipNet.Processor {
    public static class Decompressor {
        public static void Run(BinaryReader reader, BinaryWriter writer, int jobCount, int chunkSize = Constants.DEFAULT_CHUNK_SIZE,
            Threading.CancellationToken cancellationToken = null, Loggers loggers = null) {

            Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(writer, nameof(writer));
            Guard.NotZeroOrNegative(jobCount, nameof(jobCount));
            Guard.NotZeroOrNegative(chunkSize, nameof(chunkSize));

            IDefaultLogger defaultLogger = loggers?.DefaultLogger;
            IChunkLogger chunkLogger = loggers?.ChunkLogger;
            IJobLogger jobLogger = loggers?.JobLogger;

            int chunkCount = reader.ReadInt32();

            var chunks = ChunkReader.ReadChunksCompressed(reader, chunkCount)
                .AsParallel(jobCount)                
                .Do(x => chunkLogger?.LogChunk("Read", x))
                .Map(ChunkZipper.UnzipChunk)
                .Do(x => chunkLogger?.LogChunk("Proc", x))
                .AsEnumerable(cancellationToken, jobLogger);

            int index = 0;
            foreach(var chunk in chunks) {
                ChunkWriter.WriteChunk(chunk, writer, chunkSize);

                chunkLogger?.LogChunk("Write", chunk);
                defaultLogger?.LogChunksProcessed(++index, chunkCount);
            }
        }
    }
}