using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Pipeline.Channels {
    public class CompositeChannel<T> : IReadableChannel<T> {
        readonly Channel<T>[] channels;

        public Channel<T>[] Channels => channels;

        public CompositeChannel(int degreeOfParallelism) {
            Guard.NotNegative(degreeOfParallelism, nameof(degreeOfParallelism));

            channels = new Channel<T>[degreeOfParallelism];
            for(int i = 0; i < degreeOfParallelism; i++) {
                channels[i] = new Channel<T>();
            }
        }

        public bool Read(out T data) {
            data = default(T);
           
            var spinWait = new SpinWait();
            
            while(true) {
                bool finish = true;

                foreach(var channel in channels) {
                    TryReadResult channelResult = channel.TryRead(out data);

                    if(channelResult == TryReadResult.Value)
                        return true;
                    
                    if(channelResult == TryReadResult.NoValue)
                        finish = false;

                    spinWait.SpinOnce();
                }
                if(finish)
                    return false;                
            }
        }
    }
}