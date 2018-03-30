using System;

namespace ParallelZipNet.Utils {
    static class Constants {
        public const int DEFAULT_CHUNK_SIZE = (1 << 17); // 128K

        public static readonly int MAX_THREAD_COUNT = Environment.ProcessorCount;        
    }
}
