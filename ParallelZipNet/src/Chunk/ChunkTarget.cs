using System;
using System.IO;

namespace ParallelZipNet.ChunkLayer {
    public static class ChunkTarget {
        public static void WriteHeader(BinaryReader reader, BinaryWriter writer, int chunkSize, out int chunkCount) {
            chunkCount = Convert.ToInt32(reader.BaseStream.Length / chunkSize) + 1;
            writer.Write(chunkCount);
        }

        public static void WriteChunk(Chunk chunk, BinaryWriter writer, int chunkSize) {
            long position = (long)chunk.Index * chunkSize;
            writer.BaseStream.Seek(position, SeekOrigin.Begin);
            writer.Write(chunk.Data);
        }

        public static void WriteChunkCompressed(Chunk chunk, BinaryWriter writer) {
            writer.Write(chunk.Index);
            writer.Write(chunk.Data.Length);
            writer.Write(chunk.Data);
        }
    } 
}