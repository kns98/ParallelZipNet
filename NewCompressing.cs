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
        static IEnumerable<Chunk> ReadSource(StreamWrapper source) {
            bool isLastChunk;
            int chunkIndex = 0;
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

        static Chunk CompressChunk(Chunk chunk) {
            MemoryStream compressed;
            using(compressed = new MemoryStream()) {
                using(var gzip = new GZipStream(compressed, CompressionMode.Compress)) {
                    gzip.Write(chunk.Data, 0, chunk.Data.Length);                    
                    
                }
            }       
            return new Chunk(chunk.Index, compressed.ToArray());            
        }

        public static void Log(string action, Chunk chunk) {
            var threadName = Thread.CurrentThread.Name;
            if(string.IsNullOrEmpty(threadName))
                threadName = "thread MAIN";
            Console.WriteLine($"{threadName}:\t{action}\t{chunk?.Index}\t{chunk?.Data.Length}");            
        }      

        public static void Compress(StreamWrapper source, StreamWrapper dest) {
            int chunkCount = Convert.ToInt32(source.TotalBytesToRead / Constants.CHUNK_SIZE) + 1;
            dest.WriteInt32(chunkCount);

            var chunks = ReadSource(source)
                .AsParallel(Math.Max(Environment.ProcessorCount - 1, 1))
                .Do(x => Log("Read", x))
                .Map(CompressChunk)
                .Do(x => Log("Comp", x))
                .AsEnumerable();

            foreach(var chunk in chunks) {
                dest.WriteInt32(chunk.Index);
                dest.WriteInt32(chunk.Data.Length);
                dest.WriteBuffer(chunk.Data);                
                Log("Written", chunk);
            }
        }

    }

    public static class ParallelContextBuilder {
        public static ParallelContext<T> AsParallel<T>(this IEnumerable<T> enumeration, int jobNumber) where T : class {
            return new ParallelContext<T>(new LockedContext<T>(enumeration).AsEnumerable(), jobNumber);
        }
    }

    public class ParallelContext<T> where T : class  {        
        readonly IEnumerable<T> enumeration;
        readonly int jobNumber;

        public ParallelContext(IEnumerable<T> enumeration, int jobNumber) {
            this.enumeration = enumeration;
            this.jobNumber = jobNumber;
        }

        public ParallelContext<U> Map<U>(Func<T, U> transform) where U : class {
            return new ParallelContext<U>(enumeration.Select(transform), jobNumber);
        }

        public ParallelContext<T> Do(Action<T> action) {
            return Map<T>(t => { action(t); return t; });
        }

        public IEnumerable<T> AsEnumerable() {
            Job<T>[] jobs = Enumerable.Range(1, jobNumber)
                .Select(i => new Job<T>($"thread {i}", enumeration))
                .ToArray();

            while(true) {
                var jobsAlive = jobs.Where(job => job.IsAlive).ToArray();

                if(jobsAlive.Length == 0)
                    break;

                foreach(var job in jobsAlive) {
                    T result = job.Result;
                    if(result != default(T))
                        yield return result;
                    else
                        Thread.Yield();
                }
            }

            foreach(var job in jobs)
                job.Dispose();
        }
    }

    public class LockedContext<T> where T : class {
        readonly IEnumerator<T> enumerator;

        public LockedContext(IEnumerable<T> enumeration) {
            enumerator = enumeration.GetEnumerator();
        }

        public IEnumerable<T> AsEnumerable() {
            T result;
            while(true) {
                lock(enumerator) {
                    result = enumerator.MoveNext() ? enumerator.Current : null;
                }                
                if(result != null)
                    yield return result;                    
                else 
                    yield break;                    
            }
        }
    }

    public class Job<T> : IDisposable where T : class {
        readonly Queue<T> results = new Queue<T>();        
        readonly Thread thread;        
        readonly IEnumerable<T> enumeration;

        public bool IsAlive { 
            get {
                lock(results) {
                    return results.Count > 0 || thread.IsAlive;
                }
            }
        }
        public T Result {
            get {
                lock(results) {
                    return results.Count > 0 ? results.Dequeue() : null;
                }
            }
        }     

        public Job(string name, IEnumerable<T> enumeration) {
            this.enumeration = enumeration;
            thread = new Thread(Run) {
                Name = name,
                IsBackground = false
            };            
            thread.Start();            
        }

        public void Dispose() {
            thread.Join();
        }

        void Run() {           
            foreach(T result in enumeration) {
                lock(results) {
                    results.Enqueue(result);
                }
            }
        }        
    }
}

