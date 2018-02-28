using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace ParallelZipNet {
    class Program {
        const string argSrc ="@src";
        const string argDest ="@dest";

        static readonly Command helpCommand = new Command(new[] { "--help", "-h", "-?" }, _ => Help());

        static readonly List<Command> commands = new List<Command> {
            helpCommand,

            new Command(new[] { "--compress", "-c" }, new[] { argSrc, argDest }, 
                args => ProcessFile(args[argSrc], args[argDest], NewCompressing.Compress)),

            new Command(new[] { "--decompress", "-d" }, new[] { argSrc, argDest },
                args => ProcessFile(args[argSrc], args[argDest], NewCompressing.Decompress))
        };

        static int Main(string[] args) {
            try {
                if(args.Length > 0) {
                    Command command = commands.First(x => x.IsMatch(args[0]));
                    if(command == null)
                        command = helpCommand;
                    command.Run(args.Skip(1).ToArray());
                }
                return 0;
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
                return 1;
            }


            // if(args.Length > 0) {
            //     ICommand command = GetCommand(args);
            //     if(command != null && command.CheckArgs(args)) {
            //         Console.CancelKeyPress += (s, e) => {
            //             e.Cancel = true;
            //             command.ShutDown();
            //         };
            //         try {
            //             int result = command.Execute(args);
            //             if(result == 0) {
            //                 Console.WriteLine();
            //                 Console.WriteLine("Done.");
            //             }
            //             return result;
            //         }
            //         catch(CancelledException) {
            //             Console.WriteLine();
            //             Console.WriteLine("Cancelled.");
            //             return 0;
            //         }
            //         catch(Exception ex) {
            //             ConsoleHelper.LogException(ex);
            //             return 1;
            //         }
            //     }
            // }
            // ConsoleHelper.LogInvalidCommand();
            // return 1;
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
                    new Exception("Cancelled by user");
            }
            using(var stream = new StreamWrapper(srcInfo, destInfo)) {
                processor(stream, stream);
            }

        }
    }
}
