using System;
using System.Collections.Generic;
using System.Linq;

namespace ParallelZipNet.Commands {
    public class CommandProcessor {
        static Action ParseCommand(Command command, string[] args) {
            var options = new List<Option>();
            int offset = 0;            

            Option required = null;
            if(command.RequiredSection != null) {
                required = ParseSection(command.RequiredSection, args, ref offset);
                if(required != null)
                    options.Add(required);
                else
                    return null;
            }

            Option optional;
            List<Section> matched = new List<Section>();
            do {
                optional = null;                
                foreach(var section in command.OptionalSections.Except(matched)) {
                    optional = ParseSection(section, args, ref offset);
                    if(optional != null) {
                        options.Add(optional);
                        matched.Add(section);
                        break;
                    }
                }
            }
            while(optional != null && offset < args.Length);
            
            if(options.Count > 0)
                return () => command.Action(options);
            else
                return null;            
        }

        static Option ParseSection(Section section, string[] args, ref int offset) {        
            Option option = section.Parse(args.Skip(offset).ToArray());
            if(option != null)
                offset += section.Length;                
            return option;
        }

        readonly List<Command> commands = new List<Command>();

        public Command Register(Action<IEnumerable<Option>> action) {
            var command = new Command(action);
            commands.Add(command);
            return command;
        }

        public Action Parse(params string[] args) {
            foreach(var command in commands) {                
                Action action = ParseCommand(command, args);
                if(action != null)
                    return action;
            }
            throw new Exception("Unknown Command. Use --help for more information.");
        }
    }
}