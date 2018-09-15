using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ParallelZipNet.Pipeline.Channels;
using ParallelZipNet.Threading;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Pipeline {
    public class Pipeline<T> {
        static readonly Func<T, T> EmptyTransform = x => x;

        public static Pipeline<T> FromSource(string name, SourceAction<T> source) {
            var pipeline = new Pipeline<T>(new SourceChannel<T>(source), new IRoutine[0]);
            return pipeline.Pipe(name, EmptyTransform);
        }

        readonly IEnumerable<IRoutine> routines;        
        readonly IReadableChannel<T> inputChannel;

        Pipeline(IReadableChannel<T> inputChannel, IEnumerable<IRoutine> routines) {
            Guard.NotNull(inputChannel, nameof(inputChannel));
            Guard.NotNull(routines, nameof(routines));

            this.inputChannel = inputChannel;
            this.routines = routines;
        }

        public Pipeline<U> Pipe<U>(string name, Func<T, U> transform) {            
            var outputChannel = new Channel<U>();
            var routine = new Routine<T, U>(name, transform, inputChannel, outputChannel);            
            return new Pipeline<U>(outputChannel, CollectRoutines(routine));
        }
 
        public Pipeline<U> PipeMany<U>(string name, Func<T, U> transform, int degreeOfParallelism) {
            var outputChannel = new CompositeChannel<U>(degreeOfParallelism);
            var routines = outputChannel.Channels
                .Select((channel, index) => new Routine<T, U>($"{name} {index}", transform, inputChannel, channel))
                .ToArray();

            return new Pipeline<U>(outputChannel, CollectRoutines(routines));
        }

        public PipelineRunner Done(string name, Action<T> doneAction) {
            var routine = new Routine<T, T>(name, EmptyTransform, inputChannel, new TargetChannel<T>(doneAction));
            return new PipelineRunner(CollectRoutines(routine));
        }

        IEnumerable<IRoutine> CollectRoutines(params IRoutine[] routines) {
            return this.routines.Concat(routines);
        }
    }
}