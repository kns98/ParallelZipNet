using System.IO;
using System.IO.Compression;

namespace ParallelZipNet.ChunkLayer
{
    public static class ChunkConverter
    {
        public static Chunk Zip(Chunk chunk)
        {
            MemoryStream compressed;
            using (compressed = new MemoryStream())
            {
                using (var gzip = new GZipStream(compressed, CompressionMode.Compress))
                {
                    gzip.Write(chunk.Data, 0, chunk.Data.Length);
                }
            }

            return new Chunk(chunk.Index, compressed.ToArray());
        }

        public static Chunk Unzip(Chunk chunk)
        {
            MemoryStream compressed;
            MemoryStream decompressed;
            using (compressed = new MemoryStream(chunk.Data))
            {
                using (var gzip = new GZipStream(compressed, CompressionMode.Decompress))
                {
                    using (decompressed = new MemoryStream())
                    {
                        gzip.CopyTo(decompressed);
                    }
                }
            }

            return new Chunk(chunk.Index, decompressed.ToArray());
        }
    }
}