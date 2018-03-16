namespace ParallelZipNet.Logger {
    public class Loggers {
        public IDefaultLogger DefaultLogger { get; set;}
        public IChunkLogger ChunkLogger { get; set; }
        public IJobLogger JobLogger { get; set; }
    }
}