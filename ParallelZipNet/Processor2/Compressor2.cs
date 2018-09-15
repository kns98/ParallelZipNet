using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ParallelZipNet.ChunkLayer;
using ParallelZipNet.Pipeline;
using ParallelZipNet.Pipeline.Channels;
using ParallelZipNet.Processor;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Processor2 {
    public static class Compressor2 {
        public static void Run(BinaryReader reader, BinaryWriter writer) {
            int chunkSize = Constants.DEFAULT_CHUNK_SIZE;

            int chunkCount = Convert.ToInt32(reader.BaseStream.Length / chunkSize) + 1;
            writer.Write(chunkCount);

            SourceAction<Chunk> ChunkSource() {
                var readEnumerator = ChunkReader.ReadChunks(reader, chunkSize).GetEnumerator();

                return (out Chunk chunk) => {
                    bool next = readEnumerator.MoveNext();
                    chunk = next ? readEnumerator.Current : null;
                    Console.WriteLine($"Read {next}");
                    return next;
                };
            }            

            Pipeline<Chunk>
                .FromSource("read", ChunkSource())
                .PipeMany("zip", ChunkZipper.ZipChunk, Constants.DEFAULT_JOB_COUNT)
                .Done("write", chunk => {
                    ChunkWriter.WriteChunkCompressed(chunk, writer);
                    
                    Console.WriteLine($"{Task.CurrentId} Write Chunk {chunk.Index}");
                })
                .RunSync();
        }

    }
}
