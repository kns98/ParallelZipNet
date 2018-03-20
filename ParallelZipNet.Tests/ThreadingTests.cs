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
        [Fact]
        public async Task LockContext_Test() {
            var trace = new Queue<string>();

            Exception exception = null;

            Task task = Task.Run(() => {
                new[] { "A", "B", "C" }
                    .Select(x => {                        
                        trace.Enqueue($"Enter {Thread.CurrentThread.Name}");
                        Thread.Sleep(50);
                        trace.Enqueue($"Leave {Thread.CurrentThread.Name}");
                        return x;
                    })
                    .AsParallel(2)
                    .AsEnumerable(null, ex => exception = ex)
                    .ToList()
                    .ForEach(_ => {});
                })
                .WithTimeout(1000);

            await task;

            if(task.Exception != null)
                throw task.Exception;

            if(exception != null)
                throw exception;            
        }
    }
}
