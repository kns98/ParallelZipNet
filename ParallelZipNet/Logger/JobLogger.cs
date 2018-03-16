using System;

namespace ParallelZipNet.Logger {
    public interface IJobLogger {
        void LogResultsCount(int resultsCount);
    }

    public class JobLogger : IJobLogger {
        public void LogResultsCount(int resultsCount) {
            Console.Write($"{resultsCount} ");
        }
    }
}
