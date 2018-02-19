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


            // var chunks = ReadDecompressed(source)
            //     .AsSingle()
            //     .Select(CompressChunk)
            //     .AsMultiple(4);

            var chunks = ReadSource(source)
                .AsParallel(4)
                .Select(CompressChunk)
                .Select(x => {
                    NewCompressing.Log("Compressed", x);
                    return x;
                })
                .AsEnumerable();

            foreach(var chunk in chunks)
                WriteCompressed(chunk, dest);            
        }

        public static IParallelContext<Chunk> AsParallel(this IEnumerable<Chunk> source, int jobNumber) {
            return new ParallelContext<Chunk>(jobNumber, source);
        }

        public static IEnumerable<Chunk> ReadSource(StreamWrapper source) {
            bool isLastChunk;
            int chunkIndex = 0;
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

public interface IParallelContext<T> {
    IParallelContext<T> Select(Func<T, T> transform);
    IEnumerable<T> AsEnumerable();
}

public class ParallelContext<T> : IParallelContext<T> where T : class {
    readonly Job2<T>[] jobs;
    readonly List<Func<T, T>> actions = new List<Func<T, T>>();

    public ParallelContext(int jobNumber, IEnumerable<T> source) {
        jobs = new Job2<T>[jobNumber];
        for(int i = 0; i < jobs.Length; i++)
            jobs[i] = new Job2<T>($"thread {i}", actions, source);
    }

    IParallelContext<T> IParallelContext<T>.Select(Func<T, T> action) {
        actions.Add(action);
        return this;
    }    
    IEnumerable<T> IParallelContext<T>.AsEnumerable() {
        foreach(var job in jobs)
            job.Start();

        while(true) {
            var jobsAlive = jobs.Where(job => job.IsAlive).ToArray();

            if(jobsAlive.Length == 0)
                yield break;

            foreach(var job in jobsAlive) {
                T result = job.Result;
                if(result != null)
                    yield return result;
                else
                    Thread.Yield();
            }
        }
    }
}

public class Job2<T> where T : class {
    readonly string name;
    readonly IEnumerable<Func<T, T>> actions;
    readonly Queue<T> accumulator = new Queue<T>();
    readonly Thread thread;
    readonly IEnumerable<T> source;

    public bool IsAlive { 
        get {
            lock(accumulator) {
                return accumulator.Count > 0 || thread.IsAlive;
            }
        }
    }
    public T Result {
        get {
            lock(accumulator) {
                return accumulator.Count > 0 ? accumulator.Dequeue() : null;
            }
        }
    }

    public Job2(string name, IEnumerable<Func<T, T>> actions, IEnumerable<T> source) {
        this.name = name;
        this.actions = actions;
        this.source = source;
        thread = new Thread(Run) {
            Name = name,
            IsBackground = false
        };

    }
    public void Start() {
        thread.Start();
    }

    void Run() {
        IEnumerable<T> results = source;
        foreach(var action in actions)  
            results = results.Select(action);
        
        // Note:
        // It is wrong to read a source in multiple threads.
        // I need to either use a lock or introduce a dedicated thread for such reading.
        
        foreach(T result in results) {
            lock(accumulator) {
                accumulator.Enqueue(result);
            }
        }
    }        
}