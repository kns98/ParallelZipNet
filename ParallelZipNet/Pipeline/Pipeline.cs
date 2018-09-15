using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ParallelZipNet.Pipeline.Channels;

namespace ParallelZipNet.Pipeline {
        public class Pipeline<T> : IRoutine {
        public static Pipeline<T> FromSource(string name, SourceAction<T> source) {
            var pipeline = new Pipeline<T>(new SourceChannel<T>(source), new IRoutine[0]);
            return pipeline.Pipe(name, _ => _);
        }

        readonly IEnumerable<IRoutine> routines;        
        readonly IReadableChannel<T> inputChannel;

        Pipeline(IReadableChannel<T> inputChannel, IEnumerable<IRoutine> routines) {
            this.inputChannel = inputChannel;
            this.routines = routines;
        }

        public Pipeline<U> Pipe<U>(string name, Func<T, U> transform) {            
            var outputChannel = new Channel<U>();
            var routine = new Routine<T, U>(name, transform, inputChannel, outputChannel);            
            return new Pipeline<U>(outputChannel, this.routines.Concat(new[] { routine }));
        }
 
        public Pipeline<U> PipeMany<U>(string name, Func<T, U> transform, int degreeOfParallelism) {
            var outputChannel = new CompositeChannel<U>(degreeOfParallelism);
            var routines = outputChannel.Channels
                .Select((channel, index) => new Routine<T, U>($"{name} {index}", transform, inputChannel, channel))
                .ToArray();

            return new Pipeline<U>(outputChannel, this.routines.Concat(routines));
        }

        public IRoutine Done(string name, Action<T> doneAction) {
            var block = new Routine<T, T>(name, _ => _, inputChannel, new TargetChannel<T>(doneAction));
            return new Pipeline<T>(null, routines.Concat(new[] { block }));
        }

        public Task Run() {
            return Task.WhenAll(routines.Select(routine => routine.Run()));
        }
    }
}