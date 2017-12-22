using ParallelZipNet.ChunkProcessing;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System;

namespace ParallelZipNet {
    static class NewCompressing {
        public static IEnumerable<Chunk> ReadDecompressed(StreamWrapper source) {
            int chunkIndex = 0;
            bool isLastChunk;
            do {
                long bytesToRead = source.BytesToRead;
                isLastChunk = bytesToRead < Constants.CHUNK_SIZE;
                int readBytes;
                if(isLastChunk) 
                    readBytes = (int)bytesToRead;
                else
                    readBytes = Constants.CHUNK_SIZE;
                byte[] data = source.ReadBuffer(readBytes);
                yield return new Chunk(chunkIndex++, data);

            }
            while(!isLastChunk);
        }

        public static IEnumerable<Chunk> CompressChunks(this IEnumerable<Chunk> chunks) {
            foreach(var chunk in chunks) {
                MemoryStream compressed;
                using(compressed = new MemoryStream()) {
                    using(var gzip = new GZipStream(compressed, CompressionMode.Compress)) {
                        gzip.Write(chunk.Data, 0, chunk.Data.Length);                    
                    }
                }
                yield return new Chunk(chunk.Index, compressed.ToArray());
            }            
        }

        public static void WriteCompressed(this IEnumerable<Chunk> chunks, StreamWrapper dest) {
            foreach(var chunk in chunks) {               
                dest.WriteInt32(chunk.Index);
                dest.WriteInt32(chunk.Data.Length);
                dest.WriteBuffer(chunk.Data);
            }
        }

        public static void Compress(StreamWrapper source, StreamWrapper dest) {
            int chunkCount = Convert.ToInt32(source.TotalBytesToRead / Constants.CHUNK_SIZE) + 1;
            dest.WriteInt32(chunkCount);

            ReadDecompressed(source).
                CompressChunks().
                WriteCompressed(dest);
        }
    }
}