using System.IO;
using System.IO.Compression;

namespace ParallelZipNet.ChunkProcessing {
    class ChunkDecompressor : ChunkProcessorBase, IChunkProcessor {
        const string FileCorruptedMessage = "It looks like the provided gzx file is corrupted.";

        public ChunkDecompressor(StreamWrapper stream, ConcurentChunkQueue chunkQueue) 
            : base(stream, chunkQueue) {
        }

        public int GetChunkCount() {
            int chunkCount = Stream.ReadInt32();
            if(chunkCount <= 0)
                throw new InvalidDataException(FileCorruptedMessage);
            return chunkCount;
        }

        public Chunk ReadChunk() {
            int chunkIndex = Stream.ReadInt32();
            if(chunkIndex < 0)
                throw new InvalidDataException(FileCorruptedMessage);

            int chunkLength = Stream.ReadInt32();
            if(chunkLength <= 0 || chunkLength > Stream.BytesToRead)
                throw new InvalidDataException(FileCorruptedMessage);

            return new Chunk(chunkIndex, Stream.ReadBuffer(chunkLength));
        }

        public void WriteChunk(Chunk chunk, bool isFirstChunk) {
            Stream.WriteBuffer(chunk.Data, (long)chunk.Index * Constants.CHUNK_SIZE);
        }

        public void ProcessChunk(Chunk chunk) {
            MemoryStream compressed;
            MemoryStream decompressed;
            using(compressed = new MemoryStream(chunk.Data)) {
                using(var gzip = new GZipStream(compressed, CompressionMode.Decompress)) {
                    using(decompressed = new MemoryStream()) {
                        gzip.CopyTo(decompressed);                        
                    }
                }
            }
            ChunkQueue.EnqueuChunk(new Chunk(chunk.Index, decompressed.ToArray()));
        }
    }
}
