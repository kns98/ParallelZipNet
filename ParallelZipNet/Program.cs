using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using ParallelZipNet.Processor;
using ParallelZipNet.Commands;
using ParallelZipNet.Logger;
using ParallelZipNet.Utils;
using ParallelPipeline;

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
            USE_PIPELINE = "--use-pipeline",
            JOBCOUNT = "JOBCOUNT",
            JOBCOUNT_KEY = "--job-count",
            JOBCOUNT_VALUE = "JOBCOUNT_VALUE",
            CHUNKSIZE = "CHUNKSIZE",
            CHUNKSIZE_KEY = "--chunk-size",
            CHUNKSIZE_VALUE = "CHUNKSIZE_VALUE",
            PROFILE_PIPELINE = "PROFILE_PIPELINE",
            PROFILE_PIPELINE_KEY = "--profile-pipeline",
            PROFILE_PIPELINE_VALUE = "PROFILE_PIPELINE_VALUE";

        static readonly Threading.CancellationToken cancellationToken = new Threading.CancellationToken();
        static readonly ParallelPipeline.CancellationToken cancellationToken2 = new ParallelPipeline.CancellationToken();

        static readonly CommandProcessor commands = new CommandProcessor();

        static Program() {
            commands.Register(Default)                
                .Optional(HELP, new[] { "--help", "-h", "-?" }, new string[0]);

            commands.Register(Compress)
                .Required(COMPRESS, new[] { "compress", "c" }, new[] { SRC, DEST })                
                .Optional(JOBCOUNT, new[] { JOBCOUNT_KEY }, new[] { JOBCOUNT_VALUE } )
                .Optional(CHUNKSIZE, new[] { CHUNKSIZE_KEY }, new[] { CHUNKSIZE_VALUE })
                .Optional(USE_PIPELINE)
                .Optional(PROFILE_PIPELINE, new[] { PROFILE_PIPELINE_KEY }, new[] { PROFILE_PIPELINE_VALUE })
                .Optional(LOG_CHUNKS)
                .Optional(LOG_JOBS);

            commands.Register(Decompress)
                .Required(DECOMPRESS, new[] { "decompress", "d" }, new[] { SRC, DEST })
                .Optional(JOBCOUNT, new[] { JOBCOUNT_KEY }, new[] { JOBCOUNT_VALUE } )
                .Optional(CHUNKSIZE, new[] { CHUNKSIZE_KEY }, new[] { CHUNKSIZE_VALUE })
                .Optional(USE_PIPELINE)
                .Optional(PROFILE_PIPELINE, new[] { PROFILE_PIPELINE_KEY }, new[] { PROFILE_PIPELINE_VALUE })
                .Optional(LOG_CHUNKS)
                .Optional(LOG_JOBS);              
        }

        static int Main(string[] args) {
            Console.CancelKeyPress += (s, e) => {
                e.Cancel = true;
                cancellationToken.Cancel();
                cancellationToken2.Cancel();
            };
            
            try {
                commands.Parse(args)();

                Console.WriteLine();
                if(cancellationToken.IsCancelled || cancellationToken2.IsCancelled)
                    Console.WriteLine("Cancelled.");
                else
                    Console.WriteLine("Done.");                                    

                return 0;
            }
            catch(UnknownCommandException) {
                Console.WriteLine();
                Console.WriteLine("Unknown Command. Use --help for more information.");
            }
            catch(AggregateException ex) {
                Console.WriteLine();
                ex.Handle(x => {
                    Console.WriteLine(x.Message);
                    return true;
                });
            }
            catch(Exception e) {
                Console.WriteLine();
                Console.WriteLine(e.Message);
            }
            return 1;
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

            int jobCount = GetJobCount(options);
            int chunkSize = GetChunkSize(options);
            Loggers loggers = GetLoggers(options);            

            Action<BinaryReader, BinaryWriter> processor;
            if(UsePipeline(options)) {
                ProfilingType profilingType = GetProfilingType(options);
                processor = (reader, writer) => Compressor.RunAsPipeline(reader, writer, jobCount, chunkSize, cancellationToken2, loggers,
                    profilingType);
            }
            else
                processor = (reader, writer) => Compressor.RunAsEnumerable(reader, writer, jobCount, chunkSize, cancellationToken, loggers);

            ProcessFile(src, dest, processor);
        }

        static void Decompress(IEnumerable<Option> options) {
            Option decompress = options.First(x => x.Name == DECOMPRESS);
            string src = decompress.GetStringParam(SRC);
            string dest = decompress.GetStringParam(DEST);

            int jobCount = GetJobCount(options);
            int chunkSize = GetChunkSize(options);
            Loggers loggers = GetLoggers(options);            

            Action<BinaryReader, BinaryWriter> processor;
            if(UsePipeline(options)) {
                ProfilingType profilingType = GetProfilingType(options);
                processor = (reader, writer) => Decompressor.RunAsPipeline(reader, writer, jobCount, chunkSize, cancellationToken2, loggers,
                    profilingType);
            }
            else
                processor = (reader, writer) => Decompressor.RunAsEnumerable(reader, writer, jobCount, chunkSize, cancellationToken, loggers);

            ProcessFile(src, dest, processor);
        }      

        static bool UsePipeline(IEnumerable<Option> options) {
            return options.Any(x => x.Name == USE_PIPELINE);
        }

        static int GetJobCount(IEnumerable<Option> options) {
            Option jobs = options.FirstOrDefault(x => x.Name == JOBCOUNT);            
            if(jobs != null) {                        
                try {
                    return jobs.GetIntegerParam(JOBCOUNT_VALUE, 1, Constants.MAX_JOB_COUNT);
                }
                catch {                    
                }
            }
            return Constants.DEFAULT_JOB_COUNT;
        }

        static int GetChunkSize(IEnumerable<Option> options) {
            Option chunks = options.FirstOrDefault(x => x.Name == CHUNKSIZE);
            if(chunks != null) {
                try {
                    return chunks.GetIntegerParam(CHUNKSIZE_VALUE, Constants.MIN_CHUNK_SIZE, Constants.MAX_CHUNK_SIZE);
                }
                catch {                    
                }
            }
            return Constants.DEFAULT_CHUNK_SIZE;
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

        static ProfilingType GetProfilingType(IEnumerable<Option> options) {
            Option profiling = options.FirstOrDefault(x => x.Name == PROFILE_PIPELINE);
            if(profiling != null) {
                try {
                    return profiling.GetFlags<ProfilingType>(PROFILE_PIPELINE_VALUE);
                }
                catch {
                }
            }
            return ProfilingType.None;
        }

        static void ProcessFile(string src, string dest, Action<BinaryReader, BinaryWriter> processor) {
            var srcInfo = new FileInfo(src);
            if(!srcInfo.Exists)
                throw new Exception($"The \"{src}\" source file doens't exist");

            var destInfo = new FileInfo(dest);
            if(destInfo.Exists) {
                if(AskYesNoQuestion($"The \"{dest}\" file already exists, replace?")) {
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

        static bool AskYesNoQuestion(string quiestion) {
            Console.WriteLine($"{quiestion} (Y/n)");
            return Console.ReadLine().TrimStart().TrimEnd().ToUpper() != "N";
        }

        static void Help() {
            Console.WriteLine(
@"ParallelZipNet by Roman Ageev (https://github.com/RomanAgeev/ParallelZipNet)

Usage: ParallelZipNet (<Compress> | <Decompress> | <Help>)

Compress:
    (compress | c) <src> <dest> [<Options>]
    
Decompress:
    (decompress | d) <src> <dest> [<Options>]    

Options:
    --job-count <number>                                a number of concurrent threads
    --chunk-size <number>                               a size of chunk processed at once
    --log-chunks                                        log chunks details to console
    --log-jobs                                          log job details to console
    --use-pipeline                                      apply the pipeline approach
    --profile-pipeline read_write_transform_channel     profile a set of pipeline actions (considered as flags)

Help:
    --help, -h, -?");
        }
    }
}
