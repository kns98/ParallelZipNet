using System;
using System.Threading;
using ParallelZipNet.Processor;

namespace ParallelZipNet {
    public static class Logger {
        public static void Log(string action, Chunk chunk) {
            var threadName = Thread.CurrentThread.Name;
            if(string.IsNullOrEmpty(threadName))
                threadName = "thread MAIN";
            Console.WriteLine($"{threadName}:\t{action}\t{chunk?.Index}\t{chunk?.Data.Length}");            
        }      
    }
}