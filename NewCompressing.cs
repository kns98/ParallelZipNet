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
        // static byte[] data = null;
        // static Chunk chunk = null;
        //static bool isLastChunk;
        static readonly object singleLocker = new object();
        
        public static IEnumerable<Chunk> AsSingle(this IEnumerable<Chunk> chunks) {            
            Chunk result;
            var chunkEnum = chunks.GetEnumerator();            
            while(true) {
                lock(singleLocker) {
                    result = chunkEnum.MoveNext() ? chunkEnum.Current : null;
                }                
                if(result != null)
                    yield return result;                    
                else 
                    yield break;
                    
            }          
        }
                
        public static IEnumerable<Chunk> ReadDecompressed(StreamWrapper source) {
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
                Chunk chunk = new Chunk(chunkIndex++, data);
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
                byte[] compresedData = compressed.ToArray();         
                yield return new Chunk(chunk.Index, compresedData);
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

        public static IEnumerable<Chunk> AsMultiple(this IEnumerable<Chunk> chunks, int threadCount) {
            if(jobs == null) {
                jobs = new Job[threadCount];
                for(int i = 0; i < jobs.Length; i++)
                    jobs[i] = new Job($"thread {i}", chunks);
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

            foreach(var job in jobs)
                job.Join();
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
        readonly object finishedLock = new object();
        readonly Queue<Chunk> target = new Queue<Chunk>();
        readonly IEnumerable<Chunk> source;
        readonly Thread thread;

        bool finished = false;

        public bool Finished {
            get {
                lock(finishedLock) {
                    return finished;
                }
            }
        }

        public Job(string name,  IEnumerable<Chunk> source) {
            this.source = source;
            thread = new Thread(Run) {
                Name = name,
                IsBackground = false
            };
            thread.Start();
        }
        void Run() {            
            foreach(var chunk in source) {                    
                lock(targetLock) {
                    Console.WriteLine($"chunk <- {chunk.Data.Length} {chunk.Index}");
                    target.Enqueue(chunk);
                }
            }
            lock(finishedLock) {
                finished = true;
            }

        }
        public void Join() {
            thread.Join();
        }
        public Chunk GetChunk() {
            lock(targetLock) {
                Chunk chunk = target.Count > 0 ? target.Dequeue() : null;
                if(chunk != null)
                    Console.WriteLine($"chunk -> {chunk.Data.Length} {chunk.Index}");
                return chunk;
            }
        }        
    }
}