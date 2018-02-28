using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace ParallelZipNet {
    class Program {
        const string argSrc ="@src";
        const string argDest ="@dest";

        static readonly Threading.CancellationToken cancellationToken = new Threading.CancellationToken();

        static readonly Command helpCommand = new Command(new[] { "--help", "-h", "-?" }, _ => Help());

        static readonly List<Command> commands = new List<Command> {
            helpCommand,

            new Command(
                new[] { "--compress", "-c" },
                new[] { argSrc, argDest }, 
                args => ProcessFile(args[argSrc], args[argDest], (src, dest) => NewCompressing.Compress(src, dest, cancellationToken))),

            new Command(
                new[] { "--decompress", "-d" },
                new[] { argSrc, argDest },
                args => ProcessFile(args[argSrc], args[argDest], (src, dest) => NewCompressing.Decompress(src, dest, cancellationToken)))
        };

        static int Main(string[] args) {
            Console.CancelKeyPress += (s, e) => {
                e.Cancel = true;
                cancellationToken.Cancel();
            };
            
            try {
                Command command = null;
                if(args.Length > 0)
                    command = commands.FirstOrDefault(x => x.IsMatch(args[0]));
                if(command != null)
                    command.Run(args.Skip(1).ToArray(), cancellationToken);                
                else
                    helpCommand.Run();
                Console.WriteLine();
                Console.WriteLine("Done.");                                    
                return 0;
            }
            catch(CancelledException) {
                Console.WriteLine();
                Console.WriteLine("Cancelled.");
                return 0;                                
            }
            catch(Exception e) {
                Console.WriteLine();
                Console.WriteLine(e.Message);
                return 1;
            }
        }

        static void Help() {
            Console.WriteLine("TODO : Help");
        }

        static void ProcessFile(string src, string dest, Action<StreamWrapper, StreamWrapper> processor) {
            var srcInfo = new FileInfo(src);
            if(!srcInfo.Exists)
                throw new Exception($"The \"{src}\" source file doens't exist");

            var destInfo = new FileInfo(dest);
            if(destInfo.Exists) {
                if(ConsoleHelper.AskYesNoQuestion($"The \"{dest}\" file already exists, replace?")) {
                    destInfo.Delete();
                }
                else
                    throw new CancelledException();
            }
            using(var stream = new StreamWrapper(srcInfo, destInfo)) {
                processor(stream, stream);
            }

        }
    }
}
