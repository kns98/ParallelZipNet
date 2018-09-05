using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ParallelZipNet.Processor;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Processor2 {
    public static class Compressor2 {
        public static void Run(BinaryReader reader, BinaryWriter writer) {                        
            // using(var reduce = new TransferStream("reduce", writer.BaseStream))
            // using(var gzip = new GZipStream(reduce, CompressionMode.Compress))
            // // using(var gzip2 = new GZipStream(reduce, CompressionMode.Compress))
            // using(var map = new TransferStream("map", ZipChunk, ZipChunk/*, gzip2*/)) {
            //     reader.BaseStream.CopyTo(map);
            // }

            var blockReader = new Block(_ => ReadSource(reader, Constants.DEFAULT_CHUNK_SIZE));
            var blocksZip = Enumerable.Range(0, 1).Select(_ => new Block(chunk => ZipChunk((Chunk)chunk))).ToArray();
            var blockWriter = new Block(chunk => {
                writer.Write(((Chunk)chunk).Index);
                writer.Write(((Chunk)chunk).Data.Length);
                writer.Write(((Chunk)chunk).Data);
                return null;
            });
            blockReader
                .Pipe(blocksZip)
                .Pipe(blockWriter)
                .Run()
                .GetAwaiter()
                .GetResult();
        }

        static IEnumerable<Chunk> ReadSource(BinaryReader reader, int chunkSize) {
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

        static Chunk ZipChunk(Chunk chunk) {
            MemoryStream compressed;
            using(compressed = new MemoryStream()) {
                using(var gzip = new GZipStream(compressed, CompressionMode.Compress)) {
                    gzip.Write(chunk.Data, 0, chunk.Data.Length);                    
                    
                }
            }       
            return new Chunk(chunk.Index, compressed.ToArray());            
        }
    }
}
