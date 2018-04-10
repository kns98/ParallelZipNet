using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ParallelZipNet.Threading;
using ParallelZipNet.Utils;
using ParallelZipNet.Logger;

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

            var errorHandler = new ErrorHandler();

            int chunkCount = reader.ReadInt32();

            var chunks = ReadSource(reader, chunkCount)
                .AsParallel(jobCount)                
                .Do(x => chunkLogger?.LogChunk("Read", x))
                .Map(UnzipChunk)
                .Do(x => chunkLogger?.LogChunk("Proc", x))
                .AsEnumerable(cancellationToken, errorHandler.Handle, jobLogger);

            int index = 0;
            foreach(var chunk in chunks) {
                long position = (long)chunk.Index * chunkSize;
                writer.BaseStream.Seek(position, SeekOrigin.Begin);
                writer.Write(chunk.Data);

                chunkLogger?.LogChunk("Write", chunk);
                defaultLogger?.LogChunksProcessed(++index, chunkCount);
            }

            errorHandler.ThrowIfFailed();
        }

        static IEnumerable<Chunk> ReadSource(BinaryReader reader, int chunkCount) {
            if(chunkCount <= 0)
                throw new InvalidDataException();

            for(int i = 0; i < chunkCount; i++) {
                int chunkIndex = reader.ReadInt32();
                if(chunkIndex < 0)
                    throw new InvalidDataException();

                int chunkLength = reader.ReadInt32();
                long bytesToRead = reader.BaseStream.Length - reader.BaseStream.Position;
                if(chunkLength <= 0 || chunkLength > bytesToRead)
                    throw new InvalidDataException();

                yield return new Chunk(chunkIndex, reader.ReadBytes(chunkLength));                
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