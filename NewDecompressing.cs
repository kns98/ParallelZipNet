using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ParallelZipNet.ChunkProcessing;

namespace ParallelZipNet {
    static class NewDecompressing {
        public static IEnumerable<Chunk> ReadCompressed(StreamWrapper source, int chunkCount) {
            for(int i = 0; i < chunkCount; i++) {
                int chunkIndex = source.ReadInt32();
                if(chunkIndex < 0)
                    throw new InvalidDataException("FileCorruptedMessage_1");

                int chunkLength = source.ReadInt32();
                if(chunkLength <= 0 || chunkLength > source.BytesToRead)
                    throw new InvalidDataException("FileCorruptedMessage_2");

                yield return new Chunk(chunkIndex, source.ReadBuffer(chunkLength));                
            }
        }

        public static IEnumerable<Chunk> DecompressChunks(this IEnumerable<Chunk> chunks) {
            foreach(var chunk in chunks) {
                MemoryStream compressed;
                MemoryStream decompressed;
                using(compressed = new MemoryStream(chunk.Data)) {
                    using(var gzip = new GZipStream(compressed, CompressionMode.Decompress)) {
                        using(decompressed = new MemoryStream()) {
                            gzip.CopyTo(decompressed);                        
                        }
                    }
                }
                yield return new Chunk(chunk.Index, decompressed.ToArray());
            }
        }

        public static void WriteDecompressed(this IEnumerable<Chunk> chunks, StreamWrapper dest) {
            foreach(var chunk in chunks) {
                long position = chunk.Index * Constants.CHUNK_SIZE;
                dest.WriteBuffer(chunk.Data, position);
            }
        }
        public static void Decompress(StreamWrapper source, StreamWrapper dest) {
            int chunkCount = source.ReadInt32();
            if(chunkCount <= 0)
                throw new InvalidDataException("FileCorruptedMessage_3");
            ReadCompressed(source, chunkCount)
                .DecompressChunks()
                .WriteDecompressed(dest);
        }
    }
}