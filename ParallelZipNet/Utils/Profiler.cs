using System;
using System.Diagnostics;

namespace ParallelZipNet.Utils {
    [Flags]
    public enum ProfilingType {
        None = 0,
        Read = 1 << 0,
        Write = 1 << 1,        
        Transform = 1 << 2,
        Channel = 1 << 3
    }

    public class Profiler {
        readonly Stopwatch watch = new Stopwatch();        
        readonly string name;
        readonly ProfilingType profilingType;

        public Profiler(string name, ProfilingType profilingType) {
            Guard.NotNullOrWhiteSpace(name, nameof(name));

            this.name = name;
            this.profilingType = profilingType;
        }

        public void BeginWatch(ProfilingType profilingType) {
            if(this.profilingType.HasFlag(profilingType)) {
                watch.Reset();
                watch.Start();
            }
        }

        public void EndWatch(ProfilingType profilingType) {
            if(this.profilingType.HasFlag(profilingType)) {
                watch.Stop();            
                double timeMicro = (double)watch.ElapsedTicks / Stopwatch.Frequency * 1000 * 1000;
                Console.WriteLine($"{profilingType.ToString().ToUpper()} - {name} : {timeMicro}");
            }
        }            

    }
}