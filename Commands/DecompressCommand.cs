namespace ParallelZipNet.Commands {
    class DecompressCommand : FileCommand {
        protected override void Process(StreamWrapper stream) {
            NewCompressing.Decompress(stream, stream);
        }

    }
}
