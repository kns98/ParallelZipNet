using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ParallelZipNet.Threading;
using ParallelZipNet.Utils;
using ParallelZipNet.Logger;
using ParallelZipNet.ChunkLayer;

namespace ParallelZipNet.Processor {
    public static class Compressor {
        public static void Run(BinaryReader reader, BinaryWriter writer, int jobCount, int chunkSize = Constants.DEFAULT_CHUNK_SIZE,
            Threading.CancellationToken cancellationToken = null, Loggers loggers = null) {

            Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(writer, nameof(writer));
            Guard.NotZeroOrNegative(jobCount, nameof(jobCount));
            Guard.NotZeroOrNegative(chunkSize, nameof(chunkSize));

            IDefaultLogger defaultLogger = loggers?.DefaultLogger;
            IChunkLogger chunkLogger = loggers?.ChunkLogger;
            IJobLogger jobLogger = loggers?.JobLogger;            

            int chunkCount = Convert.ToInt32(reader.BaseStream.Length / chunkSize) + 1;
            writer.Write(chunkCount);
            
            var chunks = ChunkReader.ReadChunks(reader, chunkSize) 
                .AsParallel(jobCount)
                .Do(x => chunkLogger?.LogChunk("Read", x))                
                .Map(ChunkZipper.ZipChunk)
                .Do(x => chunkLogger?.LogChunk("Proc", x))                
                .AsEnumerable(cancellationToken, jobLogger);
            
            int index = 0;
            foreach(var chunk in chunks) {
                ChunkWriter.WriteChunkCompressed(chunk, writer);

                chunkLogger?.LogChunk("Write", chunk);                
                defaultLogger?.LogChunksProcessed(++index, chunkCount);
            }
        }
    }
}