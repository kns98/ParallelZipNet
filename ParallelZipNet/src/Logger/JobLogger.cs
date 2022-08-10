using System;
using ParallelContext;

namespace ParallelZipNet.Logger
{
    public class JobLogger : IJobLogger
    {
        public void LogResultsCount(int resultsCount)
        {
            Console.Write($"{resultsCount} ");
        }
    }
}