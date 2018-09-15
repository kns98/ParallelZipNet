using ParallelZipNet.Utils;

namespace ParallelZipNet.ChunkLayer {
    public class Chunk {
        public int Index { get; private set; }
        public byte[] Data { get; private set; }

        public Chunk(int index, byte[] data) {
            Guard.NotNegative(index, nameof(index));
            Guard.NotNull(data, nameof(data));

            Index = index;
            Data = data;
        }
    }
}
