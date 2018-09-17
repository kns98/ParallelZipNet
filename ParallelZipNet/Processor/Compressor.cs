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
                ChunkTarget.WriteCompressed(chunk, writer);

                chunkLogger?.LogChunk("Write", chunk);                
                defaultLogger?.LogChunksProcessed(++index, chunkCount);
            }
        }

        public static void RunAsPipeline(BinaryReader reader, BinaryWriter writer, int jobCount, int chunkSize = Constants.DEFAULT_CHUNK_SIZE,
            Threading.CancellationToken cancellationToken = null, Loggers loggers = null) {

            WriteHeader(reader, writer, chunkSize, out int chunkCount);

            SourceAction<Chunk> read = ChunkSource.ReadAction(reader, chunkSize);
            Action<Chunk> write = ChunkTarget.WriteActionCompressed(writer);

            Pipeline<Chunk>
                .FromSource("read", (out Chunk chunk) => {                    
                    bool result = read(out chunk);
                    Console.WriteLine($"read {chunk?.Index}");
                    return result;
                })
                .PipeMany("zip", (Chunk chunk) => {
                    Console.WriteLine($"zip {chunk?.Index}");
                    return ChunkConverter.Zip(chunk);
                }, jobCount)
                .Done("write", (Chunk chunk) => {
                    Console.WriteLine($"write {chunk?.Index}");
                    write(chunk);
                })
                .RunSync(cancellationToken);
        }

        static void WriteHeader(BinaryReader reader, BinaryWriter writer, int chunkSize, out int chunkCount) {
            chunkCount = Convert.ToInt32(reader.BaseStream.Length / chunkSize) + 1;
            writer.Write(chunkCount);
        }
    }
}