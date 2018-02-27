using System;
using System.Linq;
using System.Collections.Generic;

namespace ParallelZipNet.Commands {
    public class Command {
        public string[] Keys { get; set; }
        public string[] Param { get; set; }
        public Action<IDictionary<string, string>> Action { get; set; }
    }

    public class CommandRepository {
        static IDictionary<string, string> ExtractCommandParam(string[] args, string[] paramNames) {
            if(paramNames == null)
                return null;

            if(paramNames.Length != args.Length - 1)
                throw new Exception("Invalid parameters");

            var param = new Dictionary<string, string>();            
            for(int i = 0; i < paramNames.Length; i++) {
                param.Add(paramNames[i], args[i + 1]);
            }
            return param;
        }


        readonly List<Command> repository = new List<Command>();

        Command defaultCommand;

        public void Register(Command command, bool setDefault = false) {
            repository.Add(command);
            if(setDefault)
                defaultCommand = command;
        }

        public void Run(string[] args) {
            if(args.Length > 0) {
                Command command = repository.First(x => x.Keys.Contains(args[0]));
                if(command != null) {
                    var param = ExtractCommandParam(args, command.Param);
                    command.Action(param);
                    return;
                }                    
            }
            if(defaultCommand != null)
                defaultCommand.Action(null);
        }
    }
}