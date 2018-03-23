﻿using ParallelZipNet.Commands;
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

        static IEnumerable<int> Sequence(int count, int interval = 0) {
            return Enumerable.Range(1, count)
                .Select(x => { Thread.Sleep(interval); return x; });
        }

        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        public void LockContext_Test(int jobCount) {
            int sequenceLength = jobCount * 10;

            var trace = new List<string>();

            Task.Run(() => {
                Sequence(sequenceLength)
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
            .Wait(timeout)
            .Should().BeTrue("Timeout");

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
        public void NormalProcessing_Test(int jobCount) {
            int sequenceLength = jobCount * 10;

            List<int> temp = new List<int>(sequenceLength);
            List<string> results = null;

            Func<int, int> transform1 = x => x * 10;
            Func<int, string> transform2 = x => $"Value : {x}";

            Task.Run(() => {
                results = Sequence(sequenceLength, 5)
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
            .Wait(timeout)
            .Should().BeTrue("Timeout");

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
                 IEnumerable<int> values = Sequence(sequenceLength, 5)
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
        public void JobLogger_Test() {
            List<int> loggerResult = new List<int>();

            var fakeLogger = A.Fake<IJobLogger>();
            A.CallTo(() => fakeLogger.LogResultsCount(A<int>._))
                .Invokes((int resultsCount) => loggerResult.Add(resultsCount));

            Task.Run(() => {
                Sequence(10, 5)
                    .AsParallel(5)
                    .AsEnumerable(null, null, fakeLogger)
                    .ToList();
            })
            .Wait(timeout)
            .Should().BeTrue("Timeout");

            loggerResult.Should().HaveCountGreaterThan(0);            
        }

        [Fact]
        public void ErrorHandling_Test() {
            List<Exception> errors = new List<Exception>();

            Task.Run(() => {
                Sequence(10, 5)
                    .AsParallel(5)
                    .Do(_ => throw new InvalidOperationException())
                    .AsEnumerable(null, err => errors.Add(err))
                    .ToList();
            })
            .Wait(timeout)
            .Should().BeTrue("Timeout");

            errors.Should().NotBeEmpty()
                .And.ContainItemsAssignableTo<InvalidOperationException>();
        }
    }
}