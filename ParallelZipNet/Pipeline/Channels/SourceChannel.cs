using Guards;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Pipeline.Channels {
    public delegate bool SourceAction<T>(out T data);

    public class SourceChannel<T> : IReadableChannel<T> {
        readonly SourceAction<T> action;

        public SourceChannel(SourceAction<T> action) {
            Guard.NotNull(action, nameof(action));

            this.action = action;
        }

        public bool Read(out T data, Profiler profiler = null) {
            return action(out data);
        }
    }
}