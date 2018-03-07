using System.Collections.Generic;
using System.Linq;

namespace ParallelZipNet.Commands {
    public class CommandProcessor {
        readonly List<Command> commands = new List<Command>();

        Command defaultCommand;

        public void Register(Command command, bool isDefault = false) {
            commands.Add(command);
            if(isDefault)
                defaultCommand = command;
        }

        public void Run(params string[] args) {
            Command command = null;
            if(args.Length > 0)
                command = commands.FirstOrDefault(x => x.IsMatch(args[0]));
            if(command != null)
                command.Run(args.Skip(1).ToArray());                
            else if(defaultCommand != null)
                defaultCommand.Run();                
        }
    }
}