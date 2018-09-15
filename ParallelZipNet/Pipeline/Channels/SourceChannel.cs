namespace ParallelZipNet.Pipeline.Channels {
    public delegate bool SourceAction<T>(out T data);

    public class SourceChannel<T> : IReadableChannel<T> {
        readonly SourceAction<T> action;

        public SourceChannel(SourceAction<T> action) {
            this.action = action;
        }

        public bool Read(out T data) {
            return action(out data);
        }
    }
}