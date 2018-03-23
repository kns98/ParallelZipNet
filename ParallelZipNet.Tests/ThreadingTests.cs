using ParallelZipNet.Commands;
using ParallelZipNet.Threading;
using Xunit;
using FluentAssertions;
using FakeItEasy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ParallelZipNet.Logger;

namespace ParallelZipNet.Tests {
    public class ThreadingTests {
        const int timeout = 5000;

        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        public async Task LockContext_Test(int jobCount) {
            int sequenceLength = jobCount * 10;

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
                    .AsParallel(jobCount)
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

        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        public async Task NormalProcessing_Test(int jobCount) {
            int sequenceLength = jobCount * 10;

            List<int> temp = new List<int>(sequenceLength);
            List<string> results = null;

            Func<int, int> transform1 = x => x * 10;
            Func<int, string> transform2 = x => $"Value : {x}";

            Task task = Task.Run(() => {
                results = Enumerable
                    .Range(1, sequenceLength)
                    .AsParallel(jobCount)
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
            
            results.Should().BeEquivalentTo(Enumerable
                .Range(1, sequenceLength)
                .Select(transform1)
                .Select(transform2));
        }
        
        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        public async Task Cancellation_Test(int jobCount) {
            int sequenceLength = jobCount * 10;
            int cancelTreshold = jobCount * 2;
            int resultTreshold = jobCount * 3;

            var cancellationEvent = new AutoResetEvent(false);
            var cancellationToken = new Threading.CancellationToken();

            List<int> results = new List<int>();

            Task task = Task.Run(() => {
                 IEnumerable<int> values = Enumerable
                    .Range(1, sequenceLength)
                    .AsParallel(jobCount)
                    .AsEnumerable(cancellationToken);
                
                foreach(int value in values) {
                    results.Add(value);
                    if(results.Count == cancelTreshold)
                        cancellationEvent.Set();
                }                
            })
            .WithTimeout(timeout);

            Task cancellationTask = Task.Run(() => {
                cancellationEvent.WaitOne();
                cancellationToken.Cancel();
            });

            await Task.WhenAll(task, cancellationTask);

            if(task.Exception != null)
                throw task.Exception;
            
            if(cancellationTask.Exception != null)
                throw cancellationTask.Exception;

            results.Should().HaveCountLessOrEqualTo(jobCount * resultTreshold);
        }

        [Fact]    
        public async Task JobLogger_Test() {
            List<int> loggerResult = new List<int>();

            var fakeLogger = A.Fake<IJobLogger>();
            A.CallTo(() => fakeLogger.LogResultsCount(A<int>._))
                .Invokes((int resultsCount) => loggerResult.Add(resultsCount));

            Task task = Task.Run(() => {
                Enumerable
                    .Range(1, 10)
                    .Select(x => { Thread.Sleep(5); return x; })                    
                    .AsParallel(5)
                    .AsEnumerable(null, null, fakeLogger)
                    .ToList();
            })
            .WithTimeout(timeout);

            await task;

            loggerResult.Should().HaveCountGreaterThan(0);            
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
