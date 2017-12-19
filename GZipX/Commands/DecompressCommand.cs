using GZipX.ChunkProcessing;

namespace GZipX.Commands {
    class DecompressCommand : FileCommand {
        protected override IChunkProcessor CreateChunkProcessor(StreamWrapper stream, ConcurentChunkQueue chunkQueue) {
            return new ChunkDecompressor(stream, chunkQueue);
        }  
    }
}
