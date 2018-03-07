using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using ParallelZipNet.ReadWrite;
using ParallelZipNet.Processor;
using ParallelZipNet.Commands;

namespace ParallelZipNet {
    class Program {
        const string argSrc ="@src";
        const string argDest ="@dest";

        static readonly Threading.CancellationToken cancellationToken = new Threading.CancellationToken();

        static readonly CommandProcessor commands = new CommandProcessor();

        static Program() {
            commands.Register(new Command(new[] { "--help", "-h", "-?" }, _ => Help()), isDefault: true);

            commands.Register(new Command(
                new[] { "compress" },
                new[] { argSrc, argDest }, 
                args => ProcessFile(args[argSrc], args[argDest], (reader, writer) => Compressor.Run(reader, writer, cancellationToken))));

            commands.Register(new Command(
                new[] { "decompress" },
                new[] { argSrc, argDest },
                args => ProcessFile(args[argSrc], args[argDest],
                    (reader, writer) => Decompressor.Run(reader, writer, cancellationToken))));
        }

        static int Main(string[] args) {
            Console.CancelKeyPress += (s, e) => {
                e.Cancel = true;
                cancellationToken.Cancel();
            };
            
            try {
                commands.Run(args);

                Console.WriteLine();
                if(cancellationToken.IsCancelled)
                    Console.WriteLine("Cancelled.");
                else
                    Console.WriteLine("Done.");                                    

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

        static void ProcessFile(string src, string dest, Action<IBinaryReader, IBinaryWriter> processor) {
            var srcInfo = new FileInfo(src);
            if(!srcInfo.Exists)
                throw new Exception($"The \"{src}\" source file doens't exist");

            var destInfo = new FileInfo(dest);
            if(destInfo.Exists) {
                if(ConsoleHelper.AskYesNoQuestion($"The \"{dest}\" file already exists, replace?")) {
                    destInfo.Delete();
                }
                else
                    return;
            }
            using(var reader = new BinaryFileReader(srcInfo))
            using(var writer = new BinaryFileWriter(destInfo)) {
                processor(reader, writer);
            }
        }
    }
}
