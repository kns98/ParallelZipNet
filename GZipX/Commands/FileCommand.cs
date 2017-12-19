using GZipX.ChunkProcessing;
using System;
using System.IO;

namespace GZipX.Commands {
    abstract class FileCommand : ICommand {
        Engine engine;

        public bool CheckArgs(string[] args) {
            return args.Length == 3;
        }

        public int Execute(string[] args) {
            string src = args[1];            
            var srcInfo = new FileInfo(src);
            if(srcInfo.Exists) {
                string dest = args[2];
                var destInfo = new FileInfo(dest);
                if(destInfo.Exists) {
                    if(ConsoleHelper.AskYesNoQuestion($"The \"{dest}\" file already exists, replace?"))
                        destInfo.Delete();
                    else
                        throw new CancelledException();
                }
                try {
                    return ProcessFiles(srcInfo, destInfo);
                }
                catch {
                    destInfo.Delete();
                    throw;
                }
            }
            else {
                Console.WriteLine($"The \"{src}\" source file doens't exist");
                return 1;
            }
        }

        public void ShutDown() {
            if(engine != null)
                engine.ShutDown();
        }

        protected abstract IChunkProcessor CreateChunkProcessor(StreamWrapper stream, ConcurentChunkQueue chunkQueue);

        int ProcessFiles(FileInfo srcInfo, FileInfo destInfo) {
            var stream = new StreamWrapper(srcInfo, destInfo);
            var chunkQueue = new ConcurentChunkQueue();
            var chunkProcessor = CreateChunkProcessor(stream, chunkQueue);
            engine = new Engine(chunkProcessor, chunkQueue);
            try {
                engine.Run();
                return 0;
            }
            finally {
                if(engine != null) {
                    engine.Dispose();
                    engine = null;
                }
            }
        }
    }
}
