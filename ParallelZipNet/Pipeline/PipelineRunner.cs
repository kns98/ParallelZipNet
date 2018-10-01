using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ParallelZipNet.Threading;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Pipeline {
    public class PipelineRunner : IDisposable {
        readonly IEnumerable<IRoutine> routines;

        public PipelineRunner(IEnumerable<IRoutine> routines) {
            Guard.NotNull(routines, nameof(routines));
             
            this.routines = routines;
        }

        public void Run(CancellationToken cancellationToken = null) {
            if(cancellationToken == null)
                cancellationToken = new CancellationToken();

            foreach(var routine in routines)
                routine.Run(cancellationToken);
        }

        public void Dispose() {
            foreach(var routine in routines)
                routine.Dispose();            
        }
    }
}
