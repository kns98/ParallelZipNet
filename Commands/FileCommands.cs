using System;
using System.IO;

namespace ParallelZipNet.Commands {
    public static class FileCommands {
        const string argSourcePath ="@sourcePath";
        const string argDestPath ="@sourcePath";
        
        public static readonly Command Compress = new Command(
            new[] { "--compress", "-c" }, 
            new[] { argSourcePath, argDestPath },
            args => {
                FileInfo srcInfo = GetSrcInfo(args[argSourcePath]);
                FileInfo destInfo = GetDestInfo(args[argDestPath]);
                using(var stream = new StreamWrapper(srcInfo, destInfo)) {
                    NewCompressing.Compress(stream, stream);
                }
            }
        );

        public static readonly Command Decompress = new Command(
            new[] { "--decompress", "-d" }, 
            new[] { argSourcePath, argDestPath },
            args => {
                FileInfo srcInfo = GetSrcInfo(args[argSourcePath]);
                FileInfo destInfo = GetDestInfo(args[argDestPath]);
                using(var stream = new StreamWrapper(srcInfo, destInfo)) {
                    NewCompressing.Decompress(stream, stream);
                }
            }
        );

        static FileInfo GetSrcInfo(string src)  {
            var srcInfo = new FileInfo(src);
            if(!srcInfo.Exists)
                throw new Exception($"The \"{src}\" source file doens't exist");

            return srcInfo;
        }

        static FileInfo GetDestInfo(string dest) {
            var destInfo = new FileInfo(dest);
            if(destInfo.Exists) {
                if(ConsoleHelper.AskYesNoQuestion($"The \"{dest}\" file already exists, replace?")) {
                    destInfo.Delete();
                }
                else
                    new Exception("Cancelled by user");
            }
            return destInfo;
        }
    }
}