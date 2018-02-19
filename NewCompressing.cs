using ParallelZipNet.ChunkProcessing;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System;
using System.Linq;
using System.Threading;
using System.Collections;

namespace ParallelZipNet {
    static class NewCompressing {
        public static void Log(string action, Chunk chunk) {
            Console.WriteLine($"{Thread.CurrentThread.Name}:\t{action}\t{chunk.Index}\t{chunk.Data.Length}");            
        }
      
        public static IEnumerable<Chunk> AsSingle(this IEnumerable<Chunk> chunks) {            
            return new SingleContext(chunks).AsEnumerable();
        }
                
        public static IEnumerable<Chunk> ReadDecompressed(StreamWrapper source) {
            return new DecompressedReader(source).AsEnumerable();
        }

        public static Chunk CompressChunk(Chunk chunk) {
                MemoryStream compressed;
                using(compressed = new MemoryStream()) {
                    using(var gzip = new GZipStream(compressed, CompressionMode.Compress)) {
                        gzip.Write(chunk.Data, 0, chunk.Data.Length);                    
                        
                    }
                }       
                byte[] compresedData = compressed.ToArray();
                var compChunk = new Chunk(chunk.Index, compresedData);

                Log("Comp", compChunk);

                return compChunk;            
        }


        public static void WriteCompressed(Chunk chunk, StreamWrapper dest) {
            dest.WriteInt32(chunk.Index);
            dest.WriteInt32(chunk.Data.Length);
            dest.WriteBuffer(chunk.Data);                
        }

        public static IEnumerable<Chunk> AsMultiple(this IEnumerable<Chunk> chunks, int threadCount) {
            return new MultipleContext(chunks, threadCount);
        }

        public static void Compress(StreamWrapper source, StreamWrapper dest) {
            int chunkCount = Convert.ToInt32(source.TotalBytesToRead / Constants.CHUNK_SIZE) + 1;
            dest.WriteInt32(chunkCount);


            var chunks = ReadDecompressed(source)
                .AsSingle()
                .Select(CompressChunk)
                .AsMultiple(4);

            foreach(var chunk in chunks)
                WriteCompressed(chunk, dest);            
        }
    }

    class DecompressedReader {
        readonly StreamWrapper source;
        int chunkIndex = 0;
        bool isLastChunk = false;

        public DecompressedReader(StreamWrapper source) {
            this.source = source;
        }

        public IEnumerable<Chunk> AsEnumerable() {
            do {                
                long bytesToRead = source.BytesToRead;
                // if(bytesToRead == 0)
                //     yield break;
                isLastChunk = bytesToRead < Constants.CHUNK_SIZE;
                int readBytes;
                if(isLastChunk) 
                    readBytes = (int)bytesToRead;
                else
                    readBytes = Constants.CHUNK_SIZE;
                byte[] data = source.ReadBuffer(readBytes);
                Chunk chunk = new Chunk(chunkIndex++, data);

                NewCompressing.Log("Read", chunk);

                yield return chunk;

            }
            while(!isLastChunk);            
        }
    }

    class SingleContext {
        readonly object singleLocker = new object();
        IEnumerator<Chunk> chunkEnumerator;

        public SingleContext(IEnumerable<Chunk> chunks) {
            chunkEnumerator = chunks.GetEnumerator();
        }

        public IEnumerable<Chunk> AsEnumerable() {
            Chunk result;
            while(true) {
                lock(singleLocker) {
                    result = chunkEnumerator.MoveNext() ? chunkEnumerator.Current : null;
                }                
                if(result != null)
                    yield return result;                    
                else 
                    yield break;
                    
            }
        }
    }

    class MultipleContext : IEnumerable<Chunk>, IEnumerator<Chunk> {
        readonly IEnumerable<Chunk> chunks;
        readonly Job[] jobs;
        Chunk current;

        public MultipleContext(IEnumerable<Chunk> chunks, int jobCount) {
            this.chunks = chunks;
            jobs = new Job[jobCount];
            for(int i = 0; i < jobs.Length; i++)
                jobs[i] = new Job($"thread {i}", chunks);
        }


        void IDisposable.Dispose() {
            foreach(var job in jobs)
                job.Join();
        }

        IEnumerator<Chunk> IEnumerable<Chunk>.GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;

        Chunk IEnumerator<Chunk>.Current => current;
        object IEnumerator.Current => ((IEnumerator<Chunk>)this).Current;
        bool IEnumerator.MoveNext() {
            while(true) {
                var activeJobs = jobs.Where(j => !j.Finished).ToArray();
                if(activeJobs.Length == 0)
                    return false;
                foreach(var job in activeJobs) {
                    Chunk resultChunk = job.GetChunk();
                    if(resultChunk != null) {
                        current = resultChunk;
                        return true;
                    }
                    else
                        Thread.Yield();
                }
            }
        }
        void IEnumerator.Reset() { }
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
                return chunk;
            }
        }        
    }
}