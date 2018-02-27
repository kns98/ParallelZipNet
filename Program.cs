using ParallelZipNet.Commands;
using System;

namespace ParallelZipNet {
    class Program {
        static readonly CommandRepository commands = new CommandRepository();

        static Program() {
            commands.Register(GeneralCommands.Help, true);
            commands.Register(FileCommands.Compress);
            commands.Register(FileCommands.Decompress);
        }

        static int Main(string[] args) {
            try {
                commands.Run(args);
                return 0;
            }
            catch {
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

        // static ICommand GetCommand(string[] args) {
        //     switch(args[0]) {
        //         case "compress":
        //             return new CompressCommand();
        //         case "decompress":
        //             return new DecompressCommand();
        //     }
        //     return null;
        // }
    }
}
