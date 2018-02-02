using ParallelZipNet.ChunkProcessing;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System;
using System.Threading;

namespace ParallelZipNet {
    static class NewCompressing {
        public static IEnumerable<Chunk> ReadDecompressed(StreamWrapper source) {
            int chunkIndex = 0;
            bool isLastChunk;
            do {
                long bytesToRead = source.BytesToRead;
                isLastChunk = bytesToRead < Constants.CHUNK_SIZE;
                int readBytes;
                if(isLastChunk) 
                    readBytes = (int)bytesToRead;
                else
                    readBytes = Constants.CHUNK_SIZE;
                byte[] data = source.ReadBuffer(readBytes);
                yield return new Chunk(chunkIndex++, data);

            }
            while(!isLastChunk);
        }

        public static IEnumerable<Chunk> CompressChunks(this IEnumerable<Chunk> chunks) {
            foreach(var chunk in chunks) {
                MemoryStream compressed;
                using(compressed = new MemoryStream()) {
                    using(var gzip = new GZipStream(compressed, CompressionMode.Compress)) {
                        gzip.Write(chunk.Data, 0, chunk.Data.Length);                    
                    }
                }
                yield return new Chunk(chunk.Index, compressed.ToArray());
            }            
        }

        public static void WriteCompressed(this IEnumerable<Chunk> chunks, StreamWrapper dest) {
            foreach(var chunk in chunks) {
                dest.WriteInt32(chunk.Index);
                dest.WriteInt32(chunk.Data.Length);
                dest.WriteBuffer(chunk.Data);
            }
        }

        static Job[] jobs = null;
        static Thread[] threads = null;

        public static IEnumerable<Chunk> AsMultiple(this IEnumerable<Chunk> chunks, int threadCount) {
            if(jobs == null) {
                jobs = new Job[threadCount];
                for(int i = 0; i < jobs.Length; i++)
                    jobs[i] = new Job(chunks);
            }

            if(threads == null) {
                threads = new Thread[threadCount];            
                for(int i = 0; i < threads.Length; i++) {
                    threads[i] = new Thread(jobs[i].Run) { 
                        IsBackground = false
                    };
                }
                foreach(var thread in threads)
                    thread.Start();
            }

            while(true) {
                foreach(var job in jobs) {
                    Chunk resultChunk = job.GetChunk();
                    if(resultChunk != null)
                        yield return job.GetChunk();
                    else
                        Thread.Yield();
                }
            }
        }


        static readonly object singleLocker = new object();
        public static IEnumerable<Chunk> AsSingle(this IEnumerable<Chunk> chunks) {            
            lock(singleLocker) {
                foreach(var chunk in chunks)
                    yield return chunk;            
            }
        }

        public static void Compress(StreamWrapper source, StreamWrapper dest) {
            int chunkCount = Convert.ToInt32(source.TotalBytesToRead / Constants.CHUNK_SIZE) + 1;
            dest.WriteInt32(chunkCount);

            ReadDecompressed(source)
                .AsSingle()
                .CompressChunks()
                .AsMultiple(2)
                .WriteCompressed(dest);
        }
    }

    class Job {
        readonly object targetLock = new object();
        readonly Queue<Chunk> target = new Queue<Chunk>();
        readonly IEnumerable<Chunk> source;

        public Job(IEnumerable<Chunk> source) {
            this.source = source;
        }
        public void Run() {
            foreach(var chunk in source)
                target.Enqueue(chunk);
        }
        public Chunk GetChunk() {
            lock(targetLock) {
                return target.Count > 0 ? target.Dequeue() : null;
            }
        }        
    }
}