using ParallelZipNet.ChunkProcessing;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System;
using System.Linq;
using System.Threading;

namespace ParallelZipNet {
    static class NewCompressing {
        static int chunkIndex = 0;        
        static byte[] data = null;
        static Chunk chunk = null;
        static bool isLastChunk;
        
        public static IEnumerable<Chunk> ReadDecompressed(StreamWrapper source) {
            do {                
                lock(singleLocker) {
                    long bytesToRead = source.BytesToRead;
                    isLastChunk = bytesToRead < Constants.CHUNK_SIZE;
                    int readBytes;
                    if(isLastChunk) 
                        readBytes = (int)bytesToRead;
                    else
                        readBytes = Constants.CHUNK_SIZE;
                    data = source.ReadBuffer(readBytes);
                    chunk = new Chunk(chunkIndex++, data);

                }
                yield return chunk;

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
                        Name = $"thread {i}",
                        IsBackground = false
                    };
                }
                foreach(var thread in threads)
                    thread.Start();
            }

            while(true) {
                var activeJobs = jobs.Where(j => !j.Finished).ToArray();
                if(activeJobs.Length == 0)
                    break;
                foreach(var job in activeJobs) {
                    Chunk resultChunk = job.GetChunk();
                    if(resultChunk != null)
                        yield return resultChunk;
                    else
                        Thread.Yield();
                }
            }

            foreach(var thread in threads)
                thread.Join();
        }


        static readonly object singleLocker = new object();
        public static IEnumerable<Chunk> AsSingle(this IEnumerable<Chunk> chunks) {            
            var chunkEnum = chunks.GetEnumerator();
            bool next = true;
            int x = 0;
            while(true) {
                lock(singleLocker) {
                    x++;
                    next = chunkEnum.MoveNext();
                }                
                if(next)
                    yield return chunkEnum.Current;                    
                else 
                    yield break;
                    
            }          
        }

        public static void Compress(StreamWrapper source, StreamWrapper dest) {
            int chunkCount = Convert.ToInt32(source.TotalBytesToRead / Constants.CHUNK_SIZE) + 1;
            dest.WriteInt32(chunkCount);

            ReadDecompressed(source)
                //.AsSingle()
                .CompressChunks()
                .AsMultiple(2)
                .WriteCompressed(dest);
        }
    }

    class Job {
        readonly object targetLock = new object();
        readonly object finishedLock = new object();
        readonly Queue<Chunk> target = new Queue<Chunk>();
        readonly IEnumerable<Chunk> source;

        bool finished = false;

        public bool Finished {
            get {
                lock(finishedLock) {
                    return finished;
                }
            }
        }

        public Job(IEnumerable<Chunk> source) {
            this.source = source;
        }
        public void Run() {            
            foreach(var chunk in source) {                    
                lock(targetLock) {
                    Console.WriteLine($"chunk {chunk.Data.Length} {chunk.Index}");
                    target.Enqueue(chunk);
                }
            }
            lock(finishedLock) {
                finished = true;
            }

        }
        public Chunk GetChunk() {
            lock(targetLock) {
                return target.Count > 0 ? target.Dequeue() : null;
            }
        }        
    }
}