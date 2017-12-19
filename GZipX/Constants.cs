using System;

namespace GZipX {
    static class Constants {
        public const int CHUNK_SIZE = (1 << 20) * 4; // 4MB

        public static readonly int MAX_THREAD_COUNT = Environment.ProcessorCount;        
    }
}
