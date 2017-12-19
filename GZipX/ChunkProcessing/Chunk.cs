namespace GZipX.ChunkProcessing {
    class Chunk {
        public int Index { get; private set; }
        public byte[] Data { get; private set; }

        public Chunk(int index, byte[] data) {
            Index = index;
            Data = data;
        }
    }
}
