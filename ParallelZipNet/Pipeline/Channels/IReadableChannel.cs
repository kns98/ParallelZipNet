namespace ParallelZipNet.Pipeline.Channels {
    public interface IReadableChannel<T> {
        bool Read(out T data);
    }
}