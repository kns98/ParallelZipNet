using System;
using System.Collections.Concurrent;

namespace ParallelZipNet.Pipeline.Channels {
    public class Channel<T> : IReadableChannel<T>, IWritableChannel<T> {
        readonly BlockingCollection<T> collection = new BlockingCollection<T>(1000);

        public BlockingCollection<T> UnderlyingCollection => collection;

        public bool Read(out T data) {
            data = default(T);

            if(collection.IsCompleted)
                return false;

            try {
                data = collection.Take();                
            }
            catch(InvalidOperationException) {
                return false;
            }
            return true;
        }

        public void Write(T data) {
            collection.Add(data);
        }

        public void Finish() {
            collection.CompleteAdding();
        }
    }
}