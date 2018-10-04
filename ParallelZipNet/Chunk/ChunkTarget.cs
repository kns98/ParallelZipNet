using System;
using System.IO;

namespace ParallelZipNet.ChunkLayer {
    public static class ChunkTarget {
        public static void Write(this Chunk chunk, BinaryWriter writer, int chunkSize) {
            long position = (long)chunk.Index * chunkSize;
            writer.BaseStream.Seek(position, SeekOrigin.Begin);
            writer.Write(chunk.Data);
        }

        public static void WriteCompressed(this Chunk chunk, BinaryWriter writer) {
            writer.Write(chunk.Index);
            writer.Write(chunk.Data.Length);
            writer.Write(chunk.Data);
        }

        public static Action<Chunk> WriteAction(BinaryWriter writer, int chunkSize) => (Chunk chunk) => Write(chunk, writer, chunkSize);
        public static Action<Chunk> WriteActionCompressed(BinaryWriter writer) => (Chunk chunk) => WriteCompressed(chunk, writer);
    } 
}