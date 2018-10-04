using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ParallelZipNet.Threading;
using ParallelZipNet.Utils;
using ParallelZipNet.Logger;
using ParallelZipNet.ChunkLayer;
using ParallelZipNet.Pipeline;
using ParallelZipNet.Pipeline.Channels;

namespace ParallelZipNet.Processor {
    public static class Compressor {
        public static void RunAsEnumerable(BinaryReader reader, BinaryWriter writer, int jobCount, int chunkSize = Constants.DEFAULT_CHUNK_SIZE,
            Threading.CancellationToken cancellationToken = null, Loggers loggers = null) {

            Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(writer, nameof(writer));
            Guard.NotZeroOrNegative(jobCount, nameof(jobCount));
            Guard.NotZeroOrNegative(chunkSize, nameof(chunkSize));

            Console.WriteLine("Compress as Enumerable");

            IDefaultLogger defaultLogger = loggers?.DefaultLogger;
            IChunkLogger chunkLogger = loggers?.ChunkLogger;
            IJobLogger jobLogger = loggers?.JobLogger;            

            WriteHeader(reader, writer, chunkSize, out int chunkCount);
            
            var chunks = ChunkSource.Read(reader, chunkSize) 
                .AsParallel(jobCount)
                .Do(x => chunkLogger?.LogChunk("Read", x))                
                .Map(ChunkConverter.Zip)
                .Do(x => chunkLogger?.LogChunk("Proc", x))                
                .AsEnumerable(cancellationToken, jobLogger);
            
            int index = 0;
            foreach(var chunk in chunks) {
                chunk.WriteCompressed(writer);

                chunkLogger?.LogChunk("Write", chunk);                
                defaultLogger?.LogChunksProcessed(++index, chunkCount);
            }
        }

        public static void RunAsPipeline(BinaryReader reader, BinaryWriter writer, int jobCount, int chunkSize = Constants.DEFAULT_CHUNK_SIZE,
            Threading.CancellationToken cancellationToken = null, Loggers loggers = null, ProfilePipeline profile = ProfilePipeline.None) {

            Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(writer, nameof(writer));
            Guard.NotZeroOrNegative(jobCount, nameof(jobCount));
            Guard.NotZeroOrNegative(chunkSize, nameof(chunkSize));

            Console.WriteLine("Compress as Pipeline");

            IDefaultLogger defaultLogger = loggers?.DefaultLogger;
            Action<Chunk> write = ChunkTarget.WriteActionCompressed(writer);
            int index = 0; 

            WriteHeader(reader, writer, chunkSize, out int chunkCount);
            
            Pipeline<Chunk>
                .FromSource("read", ChunkSource.ReadAction(reader, chunkSize))
                .PipeMany("zip", ChunkConverter.Zip, jobCount)
                .ToTarget("write", (Chunk chunk) => {
                    write(chunk);

                    if(profile == ProfilePipeline.None)
                        defaultLogger?.LogChunksProcessed(++index, chunkCount);                    
                })
                .Run(cancellationToken, profile);
        }

        static void WriteHeader(BinaryReader reader, BinaryWriter writer, int chunkSize, out int chunkCount) {
            chunkCount = Convert.ToInt32(reader.BaseStream.Length / chunkSize) + 1;
            writer.Write(chunkCount);
        }
    }
}