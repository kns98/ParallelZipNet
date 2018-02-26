using System;
using System.Collections.Generic;

namespace ParallelZipNet.Commands {
    public static class CommandRepository {
        static readonly Dictionary<string, Action<string[]>> commands = new Dictionary<string, Action<string[]>>();

        static CommandRepository() {
            commands.Add("compress", Commands.CompressCommand);
            commands.Add("decompress", Commands.DecompressCommand);
        }
    }
}