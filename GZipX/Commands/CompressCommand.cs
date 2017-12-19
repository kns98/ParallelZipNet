using GZipX.ChunkProcessing;

namespace GZipX.Commands {
    class CompressCommand : FileCommand {
        protected override IChunkProcessor CreateChunkProcessor(StreamWrapper stream, ConcurentChunkQueue chunkQueue) {
            return new ChunkCompressor(stream, chunkQueue);
        }
    }
}
