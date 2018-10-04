using System;

namespace ParallelZipNet.Utils {
    [Flags]
    public enum ProfilePipeline {
        None = 0,
        Read = 1 << 0,
        Write = 1 << 1,        
        Transform = 1 << 2,
        Channel = 1 << 3
    }
}