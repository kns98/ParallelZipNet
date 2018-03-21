using ParallelZipNet.Commands;
using ParallelZipNet.Threading;
using Xunit;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelZipNet.Tests {
    public class ThreadingTests {
        const int timeout = 10000;

        [Theory]
        [InlineData(100)]
        public async Task LockContext_Test(int sequenceLength) {
            const int jobCount = 10;
            const int sleepTime = 10;

            var trace = new List<string>();

            Exception exception = null;

            Task task = Task.Run(() => {
                Enumerable.Range(1, sequenceLength)
                    .Select(x => {    
                        trace.Add("Enter");
                        Thread.Sleep(sleepTime);
                        trace.Add("Leave");
                        return x;
                    })
                    .AsParallel(jobCount)
                    .AsEnumerable(null, ex => exception = ex)
                    .ToList()
                    .ForEach(_ => {});
                })
                .WithTimeout(timeout);

            await task;

            if(task.Exception != null)
                throw task.Exception;

            if(exception != null)
                throw exception;

            trace.Should().HaveCount(sequenceLength * 2);
            for(int i = 0; i < trace.Count; i += 2) {
                trace[i].Should().Be("Enter");
                trace[i + 1].Should().Be("Leave");
            }
        }
    }
}
