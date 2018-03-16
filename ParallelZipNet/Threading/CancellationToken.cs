namespace ParallelZipNet.Threading {
    public class CancellationToken {
        public bool IsCancelled { get; private set; }
        
        public void Cancel() {
            IsCancelled = true;
        }
    }
}