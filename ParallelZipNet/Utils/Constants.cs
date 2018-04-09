using System;

namespace ParallelZipNet.Utils {
    static class Constants {        
        public const int DEFAULT_CHUNK_SIZE = (1 << 18); // 256KB
        public const int MIN_CHUNK_SIZE = (1 << 10); // 1KB
        public const int MAX_CHUNK_SIZE = (1 << 30); // 1GB
        
        public static readonly int DEFAULT_JOB_COUNT = Math.Max(Environment.ProcessorCount - 1, 1);
        public const int MAX_JOB_COUNT = 20;
        
    }
}
