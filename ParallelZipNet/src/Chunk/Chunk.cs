using Guards;

namespace ParallelZipNet.ChunkLayer
{
    public class Chunk
    {
        public Chunk(int index, byte[] data)
        {
            Guard.NotNegative(index, nameof(index));
            Guard.NotNull(data, nameof(data));

            Index = index;
            Data = data;
        }

        public int Index { get; }
        public byte[] Data { get; }
    }
}