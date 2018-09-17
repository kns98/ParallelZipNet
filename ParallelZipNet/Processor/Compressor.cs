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
    public static class Compressor {
        public static void RunAsEnumerable(BinaryReader reader, BinaryWriter writer, int jobCount, int chunkSize = Constants.DEFAULT_CHUNK_SIZE,
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
            
            var chunks = ChunkSource.Read(reader, chunkSize) 
                .AsParallel(jobCount)
                .Do(x => chunkLogger?.LogChunk("Read", x))                
                .Map(ChunkConverter.Zip)
                .Do(x => chunkLogger?.LogChunk("Proc", x))                
                .AsEnumerable(cancellationToken, jobLogger);
            
            int index = 0;
            foreach(var chunk in chunks) {
                ChunkTarget.WriteCompressed(chunk, writer);

                chunkLogger?.LogChunk("Write", chunk);                
                defaultLogger?.LogChunksProcessed(++index, chunkCount);
            }
        }

        public static void RunAsPipeline(BinaryReader reader, BinaryWriter writer) {
            int chunkSize = Constants.DEFAULT_CHUNK_SIZE;

            int chunkCount = Convert.ToInt32(reader.BaseStream.Length / chunkSize) + 1;
            writer.Write(chunkCount);

            Pipeline<Chunk>
                .FromSource("read", ChunkSource.ReadAction(reader, chunkSize))
                .PipeMany("zip", ChunkConverter.Zip, Constants.DEFAULT_JOB_COUNT)
                .Done("write", ChunkTarget.WriteActionCompressed(writer))
                .RunSync();
        }
    }
}