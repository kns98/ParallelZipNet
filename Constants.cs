using System;

namespace ParallelZipNet {
    static class Constants {
        public const int CHUNK_SIZE = (1 << 10) * 5;// * 4; // 4MB

        public static readonly int MAX_THREAD_COUNT = Environment.ProcessorCount;        
    }
}
