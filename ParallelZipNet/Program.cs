using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using ParallelZipNet.Processor;
using ParallelZipNet.Commands;
using ParallelZipNet.Logger;
using ParallelZipNet.Utils;

namespace ParallelZipNet {
    class Program {
        const string
            HELP = "HELP",
            COMPRESS = "COMPRESS",
            DECOMPRESS = "DECOMPRESS",
            SRC = "SRC",
            DEST = "DEST",
            LOG_CHUNKS = "--log-chunks",
            LOG_JOBS = "--log-jobs",
            JOBCOUNT = "JOBCOUNT",
            JOBCOUNT_KEY = "--job-count",
            JOBCOUNT_VALUE = "JOBCOUNT_VALUE";

        const int maxJobCount = 20;

        static readonly Threading.CancellationToken cancellationToken = new Threading.CancellationToken();

        static readonly CommandProcessor commands = new CommandProcessor();

        static Program() {
            commands.Register(Default)                
                .Optional(HELP, new[] { "--help", "-h", "-?" }, new string[0]);

            commands.Register(Compress)
                .Required(COMPRESS, new[] { "compress", "c" }, new[] { SRC, DEST })                
                .Optional(JOBCOUNT, new[] { JOBCOUNT_KEY }, new[] { JOBCOUNT_VALUE } )
                .Optional(LOG_CHUNKS)
                .Optional(LOG_JOBS);

            commands.Register(Decompress)
                .Required(DECOMPRESS, new[] { "decompress", "d" }, new[] { SRC, DEST })
                .Optional(JOBCOUNT, new[] { JOBCOUNT_KEY }, new[] { JOBCOUNT_VALUE } )
                .Optional(LOG_CHUNKS)
                .Optional(LOG_JOBS);
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

            ProcessFile(src, dest, (reader, writer) => Compressor.Run(reader, writer, GetJobCount(options), Constants.DEFAULT_CHUNK_SIZE,
                cancellationToken, GetLoggers(options)));
        }

        static void Decompress(IEnumerable<Option> options) {
            Option decompress = options.First(x => x.Name == DECOMPRESS);
            string src = decompress.GetStringParam(SRC);
            string dest = decompress.GetStringParam(DEST);
            
            ProcessFile(src, dest, (reader, writer) => Decompressor.Run(reader, writer, GetJobCount(options), Constants.DEFAULT_CHUNK_SIZE,
                cancellationToken, GetLoggers(options)));
        }

        static int GetJobCount(IEnumerable<Option> options) {
            Option jobs = options.FirstOrDefault(x => x.Name == JOBCOUNT);            
            if(jobs != null) {                        
                try {
                    return jobs.GetIntegerParam(JOBCOUNT_VALUE, 1, maxJobCount);
                }
                catch {                    
                }
            }
            return Math.Max(Environment.ProcessorCount - 1, 1);
        }

        static Loggers GetLoggers(IEnumerable<Option> options) {
            var loggers = new Loggers();            
            if(options.Any(x => x.Name == LOG_CHUNKS))
                loggers.ChunkLogger = new ChunkLogger();
            else if(options.Any(x => x.Name == LOG_JOBS))
                loggers.JobLogger = new JobLogger();
            else
                loggers.DefaultLogger = new DefaultLogger();            
            return loggers;
        }

        static void Help() {
            Console.WriteLine($"TODO : Help");
        }

        static void ProcessFile(string src, string dest, Action<BinaryReader, BinaryWriter> processor) {
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
            using(var reader = new BinaryReader(srcInfo.OpenRead()))
            using(var writer = new BinaryWriter(destInfo.OpenWrite())) {
                processor(reader, writer);
            }
        }
    }
}
