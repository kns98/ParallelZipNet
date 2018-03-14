using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Threading {
    public static class ParallelContextBuilder {
        public static ParallelContext<T> AsParallel<T>(this IEnumerable<T> enumeration, int jobNumber) where T : class {
            return new ParallelContext<T>(new LockContext<T>(enumeration).AsEnumerable(), jobNumber);
        }
    }

     public class ParallelContext<T> where T : class  {        
        readonly IEnumerable<T> enumeration;
        readonly int jobNumber;        

        public ParallelContext(IEnumerable<T> enumeration, int jobNumber) {
            Guard.NotNull(enumeration, nameof(enumeration));
            Guard.NotZeroOrNegative(jobNumber, nameof(jobNumber));
            
            this.enumeration = enumeration;
            this.jobNumber = jobNumber;
        }

        public ParallelContext<U> Map<U>(Func<T, U> transform) where U : class {
            Guard.NotNull(transform, nameof(transform));

            return new ParallelContext<U>(enumeration.Select(transform), jobNumber);
        }

        public ParallelContext<T> Do(Action<T> action) {
            Guard.NotNull(action, nameof(action));

            return Map<T>(t => { action(t); return t; });
        }

        public IEnumerable<T> AsEnumerable(CancellationToken cancellationToken = null, Action<Exception> errorHandler = null) {
            if(cancellationToken == null)
                cancellationToken = new CancellationToken();

            Job<T>[] jobs = Enumerable.Range(1, jobNumber)
                .Select(i => new Job<T>($"thread {i}", enumeration, cancellationToken))
                .ToArray();

            while(true) {
                if(cancellationToken.IsCancelled)
                    break;

                var failedJobs = jobs
                    .Where(job => job.Error != null)
                    .ToArray();

                if(failedJobs.Length > 0) {
                    cancellationToken.Cancel();
                    if(errorHandler != null)
                        foreach(var job in failedJobs)
                            errorHandler(job.Error);                    
                    break;
                }                   
                
                IEnumerable<T> results = jobs
                    .Select(job => job.GetResult())
                    .Where(r => r != null);

                foreach(T result in results)
                    yield return result;

                if(jobs.All(job => job.IsFinished))
                    break;
                else
                    Thread.Yield();                    
            }

            foreach(var job in jobs)
                job.Dispose();
        }
    }
}