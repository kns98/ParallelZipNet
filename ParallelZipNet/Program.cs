using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using ParallelZipNet.Processor;
using ParallelZipNet.Logger;
using ParallelZipNet.Utils;
using ParallelPipeline;
using ParallelCore;
using CommandParser;

namespace ParallelZipNet {
    class Program {
        static readonly CancellationToken cancellationToken = new CancellationToken();
        static readonly CommandProcessor commands = new CommandProcessor();

        static Program() {
            Command SetupSecondary(Command command) =>
                command
                    .Secondary("JobCount", it => it.WithKey("--job-count").WithInteger("Value"))
                    .Secondary("ChunkSize", it => it.WithKey("--chunk-size").WithInteger("Value"))
                    .Secondary("UsePipeline", it => it.WithKey("--use-pipeline"))
                    .Secondary("ProfilePipeline", it => it.WithKey("--profile-pipeline").WithFlags<ProfilingType>("Value"))
                    .Secondary("LogChunks", it => it.WithKey("--log-chunks"))
                    .Secondary("LogJobs", it => it.WithKey("--log-jobs"));

            Command compress = commands.Register(opt => Process(opt,
                op => op.Compress.Src,
                op => op.Compress.Dest,
                Compressor.RunAsEnumerable,
                Compressor.RunAsPipeline))
                    .Primary("Compress", it => it.WithKey("compress").WithString("Src").WithString("Dest"));

            Command decompress = commands.Register(opt => Process(opt,
                op => op.Decompress.Src,
                op => op.Decompress.Dest,
                Decompressor.RunAsEnumerable,
                Decompressor.RunAsPipeline))
                    .Primary("Decompress", it => it.WithKey("decompress").WithString("Src").WithString("Dest"));

            SetupSecondary(compress);
            SetupSecondary(decompress);

            commands.Register(Default)                
                .Secondary("Help", it => it.WithKey("--help"));
        }

        static int Main(string[] args) {
            Console.CancelKeyPress += (s, e) => {
                e.Cancel = true;
                cancellationToken.Cancel();
            };
            
            try {
                Action action = commands.Parse(args);
                if(action != null) {
                    action();

                    Console.WriteLine();
                    if(cancellationToken.IsCancelled)
                        Console.WriteLine("Cancelled.");
                    else
                        Console.WriteLine("Done.");                                    
                    return 0;
                }
                else {
                    Console.WriteLine();
                    Console.WriteLine("Unknown Command. Use --help for more information.");
                }
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

        static void Default(dynamic options) {
            if(options.Help != null)
                Help();
        }

        static void Process(dynamic options, Func<dynamic, string> getSrc, Func<dynamic, string> getDest,
            RunAsEnumerable runAsEnumerable, RunAsPipeline runAsPipeline) {

            string src = getSrc(options);
            string dest = getDest(options);

            int jobCount = options.JobCount?.Value ?? Constants.DEFAULT_JOB_COUNT;
            jobCount = Math.Max(jobCount, 1);
            jobCount = Math.Min(jobCount, Constants.MAX_JOB_COUNT);

            int chunkSize = options.ChunkSize?.Value ?? Constants.DEFAULT_CHUNK_SIZE;
            chunkSize = Math.Max(chunkSize, Constants.MIN_CHUNK_SIZE);
            chunkSize = Math.Min(chunkSize, Constants.MAX_CHUNK_SIZE);

            var loggers = new Loggers {
                DefaultLogger = new DefaultLogger(),                
                ChunkLogger = options.LogChunks != null ? new ChunkLogger() : null,
                JobLogger = options.LogJobs != null ? new JobLogger() : null
            };              

            Action<BinaryReader, BinaryWriter> processor;
            if(options.UsePipeline != null) {
                ProfilingType profilingType = options.ProfilePipeline?.Value ?? ProfilingType.None;
                processor = (reader, writer) => runAsPipeline(reader, writer, jobCount, chunkSize, cancellationToken, loggers, profilingType);
            }
            else
                processor = (reader, writer) => runAsEnumerable(reader, writer, jobCount, chunkSize, cancellationToken, loggers);

            ProcessFile(src, dest, processor);            
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
    compress <src> <dest> [<Options>]
    
Decompress:
    decompress <src> <dest> [<Options>]    

Options:
    --job-count <number>                                a number of concurrent threads
    --chunk-size <number>                               a size of chunk processed at once
    --log-chunks                                        log chunks details to console
    --log-jobs                                          log job details to console
    --use-pipeline                                      apply the pipeline approach
    --profile-pipeline read,write,transform,channel     profile a set of pipeline actions (considered as flags)

Help:
    --help");
        }
    }
}
