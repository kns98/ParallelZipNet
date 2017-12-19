namespace GZipX.ChunkProcessing {
    interface IChunkProcessor {
        long BytesToRead { get; }

        int GetChunkCount();
        Chunk ReadChunk();
        void WriteChunk(Chunk chunk, bool isFirstChunk);
        void ProcessChunk(Chunk chunk);
    }
}
