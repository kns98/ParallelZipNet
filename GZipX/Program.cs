using GZipX.Commands;
using System;

namespace GZipX {
    class Program {
        static int Main(string[] args) {
            if(args.Length > 0) {
                ICommand command = GetCommand(args);
                if(command != null && command.CheckArgs(args)) {
                    Console.CancelKeyPress += (s, e) => {
                        e.Cancel = true;
                        command.ShutDown();
                    };
                    try {
                        int result = command.Execute(args);
                        if(result == 0) {
                            Console.WriteLine();
                            Console.WriteLine("Done.");
                        }
                        return result;
                    }
                    catch(CancelledException) {
                        Console.WriteLine();
                        Console.WriteLine("Cancelled.");
                        return 0;
                    }
                    catch(Exception ex) {
                        ConsoleHelper.LogException(ex);
                        return 1;
                    }
                }
            }
            ConsoleHelper.LogInvalidCommand();
            return 1;
        }

        static ICommand GetCommand(string[] args) {
            switch(args[0]) {
                case "compress":
                    return new CompressCommand();
                case "decompress":
                    return new DecompressCommand();
            }
            return null;
        }
    }
}
