using System;
using System.Collections.Generic;
using System.IO;

namespace ParallelZipNet.Commands {
    public static class GeneralCommands {
        public static readonly Command Help = new Command(
            new[] { "--help", "-h", "-?" },
            new string[0],
            _ => Console.WriteLine("TODO : Help")
        );
    }
}