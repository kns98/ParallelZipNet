using System.Threading;

namespace ParallelZipNet.Threading {
    public class CancellationToken {
        volatile int state = 0;
        public bool IsCancelled => state == 1;
        
        public void Cancel() {
            Interlocked.Exchange(ref state, 1);
        }
    }
}