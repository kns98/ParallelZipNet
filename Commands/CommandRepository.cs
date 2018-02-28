using System;
using System.Linq;
using System.Collections.Generic;

namespace ParallelZipNet.Commands {
    public class CommandRepository {
        readonly List<Command> repository = new List<Command>();

        Command defaultCommand;

        public void Register(Command command, bool setDefault = false) {
            repository.Add(command);
            if(setDefault)
                defaultCommand = command;
        }

        public void Run(string[] args) {
            if(args.Length > 0) {
                Command command = repository.First(x => x.IsMatch(args[0]));
                if(command != null) {
                    command.Run(args.Skip(1).ToArray());
                    return;
                }                    
            }
            if(defaultCommand != null)
                defaultCommand.Run(new string[0]);
        }
    }
}