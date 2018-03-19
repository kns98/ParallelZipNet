using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ParallelZipNet.ReadWrite;
using ParallelZipNet.Threading;
using ParallelZipNet.Utils;
using ParallelZipNet.Logger;

namespace ParallelZipNet.Processor {
    public static class Decompressor {
        public static void Run(IBinaryReader reader, IBinaryWriter writer, Threading.CancellationToken cancellationToken, int jobCount,
            Loggers loggers = null) {

            Guard.NotNull(reader, nameof(reader));
            Guard.NotNull(writer, nameof(writer));
            Guard.NotZeroOrNegative(jobCount, nameof(jobCount));

            IDefaultLogger defaultLogger = loggers?.DefaultLogger;
            IChunkLogger chunkLogger = loggers?.ChunkLogger;
            IJobLogger jobLogger = loggers?.JobLogger;

            Exception error = null;

            int chunkCount = reader.ReadInt32();
            if(chunkCount <= 0)
                throw new InvalidDataException("FileCorruptedMessage_3");

            var chunks = ReadSource(reader, chunkCount)
                .AsParallel(jobCount)                
                .Do(x => chunkLogger?.LogChunk("Read", x))
                .Map(UnzipChunk)
                .Do(x => chunkLogger?.LogChunk("Proc", x))
                .AsEnumerable(cancellationToken, x => error = x, jobLogger);

            int index = 0;
            foreach(var chunk in chunks) {
                long position = (long)chunk.Index * Constants.CHUNK_SIZE;
                writer.Seek(position);
                writer.WriteBuffer(chunk.Data);

                chunkLogger?.LogChunk("Write", chunk);
                defaultLogger?.LogChunksProcessed(++index, chunkCount);
            }

            if(error != null)
                throw error;
        }

        static IEnumerable<Chunk> ReadSource(IBinaryReader reader, int chunkCount) {
            for(int i = 0; i < chunkCount; i++) {
                int chunkIndex = reader.ReadInt32();
                if(chunkIndex < 0)
                    throw new InvalidDataException("FileCorruptedMessage_1");

                int chunkLength = reader.ReadInt32();
                long bytesToRead = reader.Length - reader.Position;
                if(chunkLength <= 0 || chunkLength > bytesToRead)
                    throw new InvalidDataException("FileCorruptedMessage_2");

                yield return new Chunk(chunkIndex, reader.ReadBuffer(chunkLength));                
            }
        }

        static Chunk UnzipChunk(Chunk chunk) {
            MemoryStream compressed;
            MemoryStream decompressed;
            using(compressed = new MemoryStream(chunk.Data)) {
                using(var gzip = new GZipStream(compressed, CompressionMode.Decompress)) {
                    using(decompressed = new MemoryStream()) {
                        gzip.CopyTo(decompressed);                        
                    }
                }
            }
            return new Chunk(chunk.Index, decompressed.ToArray());            
        }                
    }
}