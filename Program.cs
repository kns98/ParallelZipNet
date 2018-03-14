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
            commands.Register(Default)                
                .Optional(HELP, new[] { "--help", "-h", "-?" }, new string[0]);

            commands.Register(Compress)
                .Required(COMPRESS, new[] { "compress", "c" }, new[] { SRC, DEST })                
                .Optional(LOG);

            commands.Register(Decompress)
                .Required(DECOMPRESS, new[] { "decompress", "d" }, new[] { SRC, DEST })
                .Optional(LOG);
        }

        static int Main(string[] args) {
            Console.CancelKeyPress += (s, e) => {
                e.Cancel = true;
                cancellationToken.Cancel();
            };
            
            try {
                commands.Parse(args)();

                Console.WriteLine();
                if(cancellationToken.IsCancelled)
                    Console.WriteLine("Cancelled.");
                else
                    Console.WriteLine("Done.");                                    

                return 0;
            }
            catch(UnknownCommandException) {
                Console.WriteLine();
                Console.WriteLine("Unknown Command. Use --help for more information.");
                return 1;
            }
            catch(Exception e) {
                Console.WriteLine();
                Console.WriteLine(e.Message);
                return 1;
            }
        }

        static void Default(IEnumerable<Option> options) {
            Option help = options.FirstOrDefault(x => x.Name == HELP);
            if(help != null)
                Help();            
        }

        static void Compress(IEnumerable<Option> options) {
            Option compress = options.First(x => x.Name == COMPRESS);
            string src = compress.GetStringParam(SRC);
            string dest = compress.GetStringParam(DEST);

            bool log = options.Any(x => x.Name == LOG);                

            ProcessFile(src, dest, (reader, writer) => Compressor.Run(reader, writer, cancellationToken, log));
        }

        static void Decompress(IEnumerable<Option> options) {
            Option decompress = options.First(x => x.Name == DECOMPRESS);
            string src = decompress.GetStringParam(SRC);
            string dest = decompress.GetStringParam(DEST);
            
            ProcessFile(src, dest, (reader, writer) => Decompressor.Run(reader, writer, cancellationToken));
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
