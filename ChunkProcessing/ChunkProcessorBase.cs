using System;

namespace ParallelZipNet.ChunkProcessing {
    abstract class ChunkProcessorBase : IDisposable {
        protected ConcurentChunkQueue ChunkQueue { get; private set; }
        protected StreamWrapper Stream { get; private set; }

        public long BytesToRead { get { return Stream.BytesToRead; } }

        protected ChunkProcessorBase(StreamWrapper stream, ConcurentChunkQueue chunkQueue) {
            Stream = stream;
            ChunkQueue = chunkQueue;
        }

        public virtual void Dispose() {
            if(Stream != null) {
                Stream.Dispose();
                Stream = null;
            }
        }
    }
}
