using System;
using System.Collections.Generic;
using System.Linq;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Commands {
    public class CommandProcessor {
        readonly List<Command> commands = new List<Command>();

        public Command Register(Action<IEnumerable<Option>> action) {
            var command = new Command(action);
            commands.Add(command);
            return command;
        }

        public Action Parse(params string[] args) {
            foreach(var command in commands) {                
                if(command.TryParse(args, out Action action))
                    return action;
            }
            throw new UnknownCommandException();            
        }
    }
}