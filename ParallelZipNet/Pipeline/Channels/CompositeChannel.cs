using System;
using System.Collections.Concurrent;
using System.Linq;

namespace ParallelZipNet.Pipeline.Channels {
    public class CompositeChannel<T> : IReadableChannel<T> {
        readonly Channel<T>[] channels;
        readonly BlockingCollection<T>[] collections;

        public Channel<T>[] Channels => channels;

        public CompositeChannel(int degreeOfParallelism) {
            channels = new Channel<T>[degreeOfParallelism];
            collections = new BlockingCollection<T>[degreeOfParallelism];

            for(int i = 0; i < degreeOfParallelism; i++) {
                var channel = new Channel<T>();
                channels[i] = channel;
                collections[i] = channel.UnderlyingCollection;
            }
        }

        public bool Read(out T data) {
            data = default(T);

            if(collections.All(collection => collection.IsCompleted))
                return false;

            try {
                BlockingCollection<T>.TakeFromAny(collections, out data);
            }
            catch(ArgumentException) {
                return false;
            }
            return true;
        }
    }
}