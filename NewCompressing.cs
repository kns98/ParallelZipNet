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

        static void WriteCompressed(Chunk chunk, StreamWrapper dest) {
            dest.WriteInt32(chunk.Index);
            dest.WriteInt32(chunk.Data.Length);
            dest.WriteBuffer(chunk.Data);                
        }

        public static void Log(string action, Chunk chunk) {
            var threadName = Thread.CurrentThread.Name;
            if(string.IsNullOrEmpty(threadName))
                threadName = "thread MAIN";
            Console.WriteLine($"{threadName}:\t{action}\t{chunk.Index}\t{chunk.Data.Length}");            
        }      

        public static void Compress(StreamWrapper source, StreamWrapper dest) {
            int chunkCount = Convert.ToInt32(source.TotalBytesToRead / Constants.CHUNK_SIZE) + 1;
            dest.WriteInt32(chunkCount);

            var chunks = ReadSource(source)
                .Select(x => {
                    NewCompressing.Log("Read", x);
                    return x;
                })
                .AsParallel(4)
                .Select(CompressChunk)
                .Select(x => {
                    NewCompressing.Log("Comp", x);
                    return x;
                })
                .AsEnumerable();

            foreach(var chunk in chunks) {
                WriteCompressed(chunk, dest);
                NewCompressing.Log("Written", chunk);
            }
        }

        public static IParallelContext<Chunk> AsParallel(this IEnumerable<Chunk> source, int jobNumber) {
            return new ParallelContext<Chunk>(jobNumber, source);
        }

    }

      public interface IParallelContext<T> {
        IParallelContext<T> Select(Func<T, T> transform);
        IParallelContext<T> Do(Action<T> action);
        IEnumerable<T> AsEnumerable();
    }

    // public interface IParallelAction {
    //     object Run(object data);
    // }

    public class ParallelContext<T> : IParallelContext<T> where T : class {
        readonly Job<T>[] jobs;
        readonly List<Func<T, T>> actions = new List<Func<T, T>>();

        public ParallelContext(int jobNumber, IEnumerable<T> source) {
            var singleContext = new SingleContext<T>(source);
            jobs = new Job<T>[jobNumber];
            for(int i = 0; i < jobs.Length; i++)
                jobs[i] = new Job<T>($"thread {i}", actions, singleContext.AsEnumerable());
        }

        IParallelContext<T> IParallelContext<T>.Select(Func<T, T> action) {
            actions.Add(action);
            return this;
        }

        IParallelContext<T> IParallelContext<T>.Do(Action<T> action) {
            // actions.Add(action);
            return this;
        }
        IEnumerable<T> IParallelContext<T>.AsEnumerable() {
            foreach(var job in jobs)
                job.Start();

            while(true) {
                var jobsAlive = jobs.Where(job => job.IsAlive).ToArray();

                if(jobsAlive.Length == 0)
                    break;

                foreach(var job in jobsAlive) {
                    T result = job.Result;
                    if(result != null)
                        yield return result;
                    else
                        Thread.Yield();
                }
            }

            foreach(var job in jobs)
                job.Finish();
        }
    }

    public class SingleContext<T> where T : class {
        readonly IEnumerator<T> sourceEnum;

        public SingleContext(IEnumerable<T> source) {
            sourceEnum = source.GetEnumerator();
        }

        public IEnumerable<T> AsEnumerable() {
            T result;
            while(true) {
                lock(sourceEnum) {
                    result = sourceEnum.MoveNext() ? sourceEnum.Current : null;
                }                
                if(result != null)
                    yield return result;                    
                else 
                    yield break;                    
            }
        }
    }

    public class Job<T> where T : class {
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

        public Job(string name, IEnumerable<Func<T, T>> actions, IEnumerable<T> source) {
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

        public void Finish() {
            thread.Join();
        }

        void Run() {
            IEnumerable<T> results = source;
            foreach(var action in actions)  
                results = results.Select(action);            
            
            foreach(T result in results) {
                lock(accumulator) {
                    accumulator.Enqueue(result);
                }
            }
        }        
    }
}

