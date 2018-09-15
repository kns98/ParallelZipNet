using System;
using System.Threading;
using ParallelZipNet.Processor;
using ParallelZipNet.Utils;
using ParallelZipNet.ChunkLayer;

namespace ParallelZipNet.Logger {
    public interface IChunkLogger {
        void LogChunk(string action, Chunk chunk);
    }

    public class ChunkLogger : IChunkLogger {
        public void LogChunk(string action, Chunk chunk) {
            Guard.NotNull(chunk, nameof(chunk));

            var threadName = Thread.CurrentThread.Name;
            if(string.IsNullOrEmpty(threadName))
                threadName = "*";
            Console.WriteLine($"{threadName} ->\t{action}\t{chunk.Index}\t{chunk.Data.Length}");
        }
    }
}