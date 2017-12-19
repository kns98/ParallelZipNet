using System.Collections.Generic;

namespace ParallelZipNet.ChunkProcessing {
    class ConcurentChunkQueue {
        readonly object queueLock = new object();
        readonly Queue<Chunk> queue = new Queue<Chunk>();

        public void EnqueuChunk(Chunk chunk) {
            lock(queueLock) {
                queue.Enqueue(chunk);
            }
        }
        public Chunk DequeueChunk() {
            lock(queueLock) {
                return queue.Count > 0 ? queue.Dequeue() : null;
            }
        }
    }
}
