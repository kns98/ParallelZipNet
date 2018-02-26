using ParallelZipNet.ChunkProcessing;

namespace ParallelZipNet.Commands {
    class DecompressCommand : FileCommand {
        protected override IChunkProcessor CreateChunkProcessor(StreamWrapper stream, ConcurentChunkQueue chunkQueue) {
            return new ChunkDecompressor(stream, chunkQueue);
        }  

        protected override void Process(StreamWrapper stream) {
            NewCompressing.Decompress(stream, stream);
        }

    }
}
