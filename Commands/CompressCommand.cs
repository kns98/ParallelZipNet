
namespace ParallelZipNet.Commands {
    class CompressCommand : FileCommand {
        protected override void Process(StreamWrapper stream) {
            NewCompressing.Compress(stream, stream);
        }
    }
}
