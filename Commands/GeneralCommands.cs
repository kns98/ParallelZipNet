using System;
using System.Collections.Generic;
using System.IO;

namespace ParallelZipNet.Commands {
    public static class GeneralCommands {
        public static readonly Command Help = new Command {
            Keys = new[] { "--help", "-h", "-?" }, 
            Action = _ => {
                Console.WriteLine("TODO : Help");
            }
        };
    }
}