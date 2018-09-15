using System.IO;

namespace ParallelZipNet.ChunkLayer {
    public static class ChunkWriter {
        public static void WriteChunkCompressed(Chunk chunk, BinaryWriter writer) {
            writer.Write(chunk.Index);
            writer.Write(chunk.Data.Length);
            writer.Write(chunk.Data);
        }

        public static void WriteChunk(Chunk chunk, BinaryWriter writer, int chunkSize) {
            long position = (long)chunk.Index * chunkSize;
            writer.BaseStream.Seek(position, SeekOrigin.Begin);
            writer.Write(chunk.Data);
        }
    } 
}