﻿using System;
using System.IO;
using CommandParser;
using ParallelCore;
using ParallelPipeline;
using ParallelZipNet.Logger;

namespace ParallelZipNet
{
    internal class Program
    {
        private static readonly CancellationToken _cancellationToken = new CancellationToken();
        private static readonly CommandProcessor _commands = new CommandProcessor();

        static Program()
        {
            Command SetupSecondary(Command command)
            {
                return command
                    .Secondary("JobCount", it => it.WithKey("--job-count").WithInteger("Value"))
                    .Secondary("ChunkSize", it => it.WithKey("--chunk-size").WithInteger("Value"))
                    .Secondary("UsePipeline", it => it.WithKey("--use-pipeline"))
                    .Secondary("ProfilePipeline",
                        it => it.WithKey("--profile-pipeline").WithFlags<ProfilingType>("Value"))
                    .Secondary("LogChunks", it => it.WithKey("--log-chunks"))
                    .Secondary("LogJobs", it => it.WithKey("--log-jobs"));
            }

            var compress = _commands.Register(opt => Process(opt,
                    op => op.Compress.Src,
                    op => op.Compress.Dest,
                    RunAsEnumerable.Compress,
                    RunAsPipeline.Compress))
                .Primary("Compress", it => it.WithKey("compress").WithString("Src").WithString("Dest"));

            var decompress = _commands.Register(opt => Process(opt,
                    op => op.Decompress.Src,
                    op => op.Decompress.Dest,
                    RunAsEnumerable.Decompress,
                    RunAsPipeline.Decompress))
                .Primary("Decompress", it => it.WithKey("decompress").WithString("Src").WithString("Dest"));

            SetupSecondary(compress);
            SetupSecondary(decompress);

            _commands.Register(Default)
                .Secondary("Help", it => it.WithKey("--help"));
        }

        private static int Main(string[] args)
        {
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                _cancellationToken.Cancel();
            };

            try
            {
                var action = _commands.Parse(args);
                if (action != null)
                {
                    action();

                    Console.WriteLine();
                    if (_cancellationToken.IsCancelled)
                        Console.WriteLine("Cancelled.");
                    else
                        Console.WriteLine("Done.");
                    return 0;
                }

                Console.Error.WriteLine();
                Console.Error.WriteLine("Unknown Command. Use --help for more information.");
            }
            catch (AggregateException ex)
            {
                Console.Error.WriteLine();
                ex.Handle(x =>
                {
                    Console.Error.WriteLine(x.Message);
                    return true;
                });
            }
            catch (Exception e)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine(e.Message);
            }

            return 1;
        }

        private static void Default(dynamic options)
        {
            if (options.Help != null)
                Help();
        }

        private static void Process(dynamic options, Func<dynamic, string> getSrc, Func<dynamic, string> getDest,
            RunAsEnumerable.Action runAsEnumerable, RunAsPipeline.Action runAsPipeline)
        {
            string src = getSrc(options);
            string dest = getDest(options);

            int jobCount = options.JobCount?.Value ?? Constants.DEFAULT_JOB_COUNT;
            jobCount = Math.Max(jobCount, 1);
            jobCount = Math.Min(jobCount, Constants.MAX_JOB_COUNT);

            int chunkSize = options.ChunkSize?.Value ?? Constants.DEFAULT_CHUNK_SIZE;
            chunkSize = Math.Max(chunkSize, Constants.MIN_CHUNK_SIZE);
            chunkSize = Math.Min(chunkSize, Constants.MAX_CHUNK_SIZE);

            var loggers = new Loggers();
            if (options.LogChunks != null)
                loggers.ChunkLogger = new ChunkLogger();
            else if (options.LogJobs != null)
                loggers.JobLogger = new JobLogger();
            else
                loggers.DefaultLogger = new DefaultLogger();

            Action<BinaryReader, BinaryWriter> processor;
            if (options.UsePipeline != null)
            {
                ProfilingType profilingType = options.ProfilePipeline?.Value ?? ProfilingType.None;
                processor = (reader, writer) => runAsPipeline(reader, writer, jobCount, chunkSize, _cancellationToken,
                    loggers, profilingType);
            }
            else
            {
                processor = (reader, writer) =>
                    runAsEnumerable(reader, writer, jobCount, chunkSize, _cancellationToken, loggers);
            }

            ProcessFile(src, dest, processor);
        }

        private static void ProcessFile(string src, string dest, Action<BinaryReader, BinaryWriter> processor)
        {
            var srcInfo = new FileInfo(src);
            if (!srcInfo.Exists)
                throw new Exception($"The \"{src}\" source file doens't exist");

            var destInfo = new FileInfo(dest);
            if (destInfo.Exists)
            {
                if (AskYesNoQuestion($"The \"{dest}\" file already exists, replace?"))
                    destInfo.Delete();
                else
                    return;
            }

            using (var reader = new BinaryReader(srcInfo.OpenRead()))
            using (var writer = new BinaryWriter(destInfo.OpenWrite()))
            {
                processor(reader, writer);
            }
        }

        private static bool AskYesNoQuestion(string quiestion)
        {
            Console.WriteLine($"{quiestion} (Y/n)");
            return Console.ReadLine().TrimStart().TrimEnd().ToUpper() != "N";
        }

        private static void Help()
        {
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