using ParallelZipNet.ChunkProcessing;
using ParallelZipNet.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ParallelZipNet {
    class Engine : IDisposable {
        readonly ConcurentChunkQueue chunkQueue;
        ThreadPool<Chunk> pool;
        IChunkProcessor chunkProcessor;
        int keepRunning = 0;

        public Engine(IChunkProcessor chunkProcessor, ConcurentChunkQueue chunkQueue) {
            this.chunkProcessor = chunkProcessor;
            this.chunkQueue = chunkQueue;
            pool = new ThreadPool<Chunk>(Constants.MAX_THREAD_COUNT);
        }

        public void Dispose() {
            var chunkProcessorDisp = chunkProcessor as IDisposable;
            if(chunkProcessorDisp != null) {
                chunkProcessorDisp.Dispose();
                chunkProcessor = null;
            }
            if(pool != null) {
                pool.ShutDown();
                pool = null;
            }
        }

        public void ShutDown() {
            Interlocked.Increment(ref keepRunning);
        }

        public void Run() {
            int chunkCount = chunkProcessor.GetChunkCount();
            int chunksProcessed = 0;

            while(keepRunning == 0 && chunksProcessed < chunkCount) {
                List<Exception> errors = pool.HandleErrors().ToList();
                if(errors.Count > 0)
                    throw new AggregateException(errors);

                Chunk chunkToWrite = chunkQueue.DequeueChunk();
                if(chunkToWrite != null) {
                    chunkProcessor.WriteChunk(chunkToWrite, chunksProcessed++ == 0);
                    ConsoleHelper.LogChunk(chunksProcessed, chunkCount);
                }
                else if(pool.Ready) {
                    long bytesToRead = chunkProcessor.BytesToRead;
                    if(bytesToRead > 0) {
                        Chunk chunk = chunkProcessor.ReadChunk();
                        pool.EnqueueWork(chunk, chunkProcessor.ProcessChunk);
                    }
                }
            }
            if(keepRunning > 0)
                throw new CancelledException();
        }
    }    
}
