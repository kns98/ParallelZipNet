using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ParallelZipNet.Threading;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Pipeline {
    public class PipelineRunner {
         readonly IEnumerable<IRoutine> routines;

         public PipelineRunner(IEnumerable<IRoutine> routines) {
             Guard.NotNull(routines, nameof(routines));
             
             this.routines = routines;
         }

        public Task RunAsync(CancellationToken cancellationToken = null) {
            if(cancellationToken == null)
                cancellationToken = new CancellationToken();

            return Task.WhenAll(routines.Select(routine => routine.Run(cancellationToken)));
        }

        public void RunSync(CancellationToken cancellationToken = null) {
            RunAsync(cancellationToken).Wait();
        }
    }
}
