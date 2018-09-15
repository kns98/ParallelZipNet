using System.Collections.Generic;
using System.IO;

namespace ParallelZipNet.ChunkLayer {
    public static class ChunkReader {
        public static IEnumerable<Chunk> ReadChunks(BinaryReader reader, int chunkSize) {
            bool isLastChunk;
            int chunkIndex = 0;
            do {                
                long bytesToRead = reader.BaseStream.Length - reader.BaseStream.Position;
                isLastChunk = bytesToRead < chunkSize;
                int readBytes;
                if(isLastChunk) 
                    readBytes = (int)bytesToRead;
                else
                    readBytes = chunkSize;
                byte[] data = reader.ReadBytes(readBytes);
                yield return new Chunk(chunkIndex++, data);

            }
            while(!isLastChunk);            
        }

        public static IEnumerable<Chunk> ReadChunksCompressed(BinaryReader reader, int chunkCount) {
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
    }
}