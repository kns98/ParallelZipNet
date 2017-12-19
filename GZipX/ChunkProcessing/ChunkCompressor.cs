using System;
using System.IO;
using System.IO.Compression;

namespace GZipX.ChunkProcessing {
    class ChunkCompressor : ChunkProcessorBase, IChunkProcessor {
        int chunkIndex;

        public ChunkCompressor(StreamWrapper stream, ConcurentChunkQueue chunkQueue) 
            : base(stream, chunkQueue) {
        }

        public int GetChunkCount() {
            return Convert.ToInt32(Stream.TotalBytesToRead / Constants.CHUNK_SIZE) + 1;
        }

        public Chunk ReadChunk() {
            long bytesToRead = Stream.BytesToRead;
            int readBytes = bytesToRead < Constants.CHUNK_SIZE ? (int)bytesToRead : Constants.CHUNK_SIZE;
            byte[] data = Stream.ReadBuffer(readBytes);
            return new Chunk(chunkIndex++, data);
        }

        public void WriteChunk(Chunk chunk, bool isFirstChunk) {
            if(isFirstChunk)
                Stream.WriteInt32(GetChunkCount());
            Stream.WriteInt32(chunk.Index);
            Stream.WriteInt32(chunk.Data.Length);
            Stream.WriteBuffer(chunk.Data);
        }

        public void ProcessChunk(Chunk chunk) {
            MemoryStream compressed;
            using(compressed = new MemoryStream()) {
                using(var gzip = new GZipStream(compressed, CompressionMode.Compress)) {
                    gzip.Write(chunk.Data, 0, chunk.Data.Length);                    
                }
            }
            ChunkQueue.EnqueuChunk(new Chunk(chunk.Index, compressed.ToArray()));
        }
    }
}
