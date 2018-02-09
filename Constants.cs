using System;

namespace ParallelZipNet {
    static class Constants {
        public const int CHUNK_SIZE = (1 << 14); // 4MB

        public static readonly int MAX_THREAD_COUNT = Environment.ProcessorCount;        
    }
}
