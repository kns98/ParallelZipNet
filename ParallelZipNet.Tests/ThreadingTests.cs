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
        const int timeout = 5000;

        [Fact]
        public async Task LockContext_Test() {
            const int sequenceLength = 100;

            var trace = new List<string>();

            Task task = Task.Run(() => {
                Enumerable
                    .Range(1, sequenceLength)
                    .Select(x => {    
                        trace.Add("Enter");
                        Thread.Sleep(10);
                        trace.Add("Leave");
                        return x;
                    })
                    .AsParallel(5)
                    .AsEnumerable()
                    .ToList();
                })
                .WithTimeout(timeout);

            await task;

            if(task.Exception != null)
                throw task.Exception;

            trace.Should().HaveCount(sequenceLength * 2);
            for(int i = 0; i < trace.Count; i += 2) {
                trace[i].Should().Be("Enter");
                trace[i + 1].Should().Be("Leave");
            }
        }

        [Fact]
        public async Task NormalProcessing_Test() {
            const int sequenceLength = 100;

            List<int> temp = new List<int>(sequenceLength);
            List<string> result = null;

            Func<int, int> transform1 = x => x * 10;
            Func<int, string> transform2 = x => $"Value : {x}";

            Task task = Task.Run(() => {
                result = Enumerable
                    .Range(1, sequenceLength)
                    .AsParallel(5)
                    .Map(transform1)
                    .Do(x => {                        
                        lock(temp) {
                            temp.Add(x);
                        }
                    })
                    .Map(transform2)
                    .AsEnumerable()
                    .ToList();
            })
            .WithTimeout(timeout);

            await task;

            if(task.Exception != null)
                throw task.Exception;

            temp.Should().BeEquivalentTo(Enumerable
                .Range(1, sequenceLength)
                .Select(transform1));
            
            result.Should().BeEquivalentTo(Enumerable
                .Range(1, sequenceLength)
                .Select(transform1)
                .Select(transform2));
        }

        [Fact]
        public async Task ErrorHandling_Test() {
            List<Exception> errors = new List<Exception>();

            Task task = Task.Run(() => {
                Enumerable
                    .Range(1, 10)                    
                    .AsParallel(5)
                    .Do(_ => throw new InvalidOperationException())
                    .AsEnumerable(null, err => errors.Add(err))
                    .ToList();
            })
            .WithTimeout(timeout);

            await task;

            if(task.Exception != null)
                throw task.Exception;

            errors.Should().NotBeEmpty()
                .And.ContainItemsAssignableTo<InvalidOperationException>();
        }
    }
}
