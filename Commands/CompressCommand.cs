using ParallelZipNet.ChunkProcessing;

namespace ParallelZipNet.Commands {
    class CompressCommand : FileCommand {
        protected override IChunkProcessor CreateChunkProcessor(StreamWrapper stream, ConcurentChunkQueue chunkQueue) {
            return new ChunkCompressor(stream, chunkQueue);
        }
    }
}
