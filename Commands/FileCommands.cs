using System;
using System.IO;

namespace ParallelZipNet.Commands {
    public static class FileCommands {
        public static readonly Command Compress = new Command {
            Keys = new[] { "--compress", "-c" }, 
            Param = new[] { "@sourcePath", "@destPath" },
            Action = args => {
                FileInfo srcInfo = GetSrcInfo(args["@sourcePath"]);
                FileInfo destInfo = GetDestInfo(args["@destPath"]);
                using(var stream = new StreamWrapper(srcInfo, destInfo)) {
                    NewCompressing.Compress(stream, stream);
                }
            }
        };

        public static readonly Command Decompress = new Command {
            Keys = new[] { "--decompress", "-d" }, 
            Param = new[] { "@sourcePath", "@destPath" },
            Action = args => {
                FileInfo srcInfo = GetSrcInfo(args["@sourcePath"]);
                FileInfo destInfo = GetDestInfo(args["@destPath"]);
                using(var stream = new StreamWrapper(srcInfo, destInfo)) {
                    NewCompressing.Decompress(stream, stream);
                }
            }
        };

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