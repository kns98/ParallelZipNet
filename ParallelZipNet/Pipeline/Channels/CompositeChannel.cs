using System;
using System.Collections.Concurrent;
using System.Linq;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Pipeline.Channels {
    public class CompositeChannel<T> : IReadableChannel<T> {
        readonly Channel<T>[] channels;
        // readonly BlockingCollection<T>[] collections;

        public Channel<T>[] Channels => channels;

        public CompositeChannel(int degreeOfParallelism) {
            Guard.NotNegative(degreeOfParallelism, nameof(degreeOfParallelism));

            channels = new Channel<T>[degreeOfParallelism];
            // collections = new BlockingCollection<T>[degreeOfParallelism];

            for(int i = 0; i < degreeOfParallelism; i++) {
                channels[i] = new Channel<T>();
                // var channel = new Channel<T>();
                // channels[i] = channel;
                // collections[i] = channel.UnderlyingCollection;
            }
        }

        public bool Read(out T data) {
            data = default(T);

            while(true) {
                bool exit = true;
                foreach(var channel in channels) {
                    bool? channelResult = channel.Read(out data, 10);
                    if(channelResult.HasValue) {
                        if(channelResult.Value)
                            return true;
                    }
                    else
                        exit = false;
                }
                if(exit)
                    return false;
            }

            // bool result = false;


            // if(collections.All(collection => collection.IsCompleted))
            //     return false;

            // try {
            //     BlockingCollection<T>.TakeFromAny(collections, out data);
            // }
            // catch(ArgumentException) {
            //     return false;
            // }
            // return true;
        }
    }
}