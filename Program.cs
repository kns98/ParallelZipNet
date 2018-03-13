using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using ParallelZipNet.ReadWrite;
using ParallelZipNet.Processor;
using ParallelZipNet.Commands;

namespace ParallelZipNet {
    class Program {
        const string
            HELP = "HELP",
            COMPRESS = "COMPRESS",
            DECOMPRESS = "DECOMPRESS",
            SRC = "SRC",
            DEST = "DEST",
            LOG = "--log";

        static readonly Threading.CancellationToken cancellationToken = new Threading.CancellationToken();

        static readonly CommandProcessor commands = new CommandProcessor();

        static Program() {
            commands.Register(opts => {
                Option help = opts.FirstOrDefault(x => x.Name == HELP);
                if(help != null)
                    Help();
            })                
            .Optional(HELP, new[] { "--help", "-h", "-?" }, new string[0]);

            commands.Register(opts => {
                Option compress = opts.First(x => x.Name == COMPRESS);
                string src = compress.GetStringParam(SRC);
                string dest = compress.GetStringParam(DEST);

                bool log = opts.Any(x => x.Name == LOG);                

                ProcessFile(src, dest, (reader, writer) => Compressor.Run(reader, writer, cancellationToken, log));
            })
            .Required(COMPRESS, new[] { "compress", "c" }, new[] { SRC, DEST })                
            .Optional(LOG);

            commands.Register(opts => {
                Option compress = opts.First(x => x.Name == DECOMPRESS);
                string src = compress.GetStringParam(SRC);
                string dest = compress.GetStringParam(DEST);
                
                ProcessFile(src, dest, (reader, writer) => Decompressor.Run(reader, writer, cancellationToken));
            })
            .Required(DECOMPRESS, new[] { "decompress", "d" }, new[] { SRC, DEST })
            .Optional(LOG);
        }

        static int Main(string[] args) {
            Console.CancelKeyPress += (s, e) => {
                e.Cancel = true;
                cancellationToken.Cancel();
            };
            
            try {
                Action action = commands.Parse(args);
                action();

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
            Console.WriteLine($"TODO : Help");
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
