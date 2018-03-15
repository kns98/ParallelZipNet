using System;

namespace ParallelZipNet.Logger {
    public interface IDefaultLogger {
        void LogChunksProcessed(int index, int count);
    }

    public class DefaultLogger : IDefaultLogger {
        public void LogChunksProcessed(int index, int count) {
            Console.Write($"\r{new string(' ', Console.WindowWidth)}\r");
            Console.Write($"{index} of {count} chunks processed");                        
        }
    }
}