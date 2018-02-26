using System.IO;

namespace ParallelZipNet.Commands {
    public static class Commands {
        static FileInfo GetSrcInfo(string src)  {
            var srcInfo = new FileInfo(src);
            return srcInfo.Exists ? srcInfo : null;
        }

        static FileInfo GetDestInfo(string dest) {
            var destInfo = new FileInfo(dest);
            if(destInfo.Exists) {
                if(ConsoleHelper.AskYesNoQuestion($"The \"{dest}\" file already exists, replace?")) {
                    destInfo.Delete();
                }
                else
                    return null;
            }
            return destInfo;
        }

        public static void CompressCommand(string[] args) {
            FileInfo srcInfo = GetSrcInfo(args[1]);
            FileInfo destInfo = GetDestInfo(args[2]);
            using(var stream = new StreamWrapper(srcInfo, destInfo)) {
                NewCompressing.Compress(stream, stream);
            }            
        }

        public static void DecompressCommand(string[] args) {
            FileInfo srcInfo = GetSrcInfo(args[1]);
            FileInfo destInfo = GetDestInfo(args[2]);
            using(var stream = new StreamWrapper(srcInfo, destInfo)) {
                NewCompressing.Decompress(stream, stream);
            }                        
        }        
    }
}