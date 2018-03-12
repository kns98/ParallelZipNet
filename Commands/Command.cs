using System;
using System.Linq;
using System.Collections.Generic;

namespace ParallelZipNet.Commands {
    public class Command {
        readonly CommandSection required;
        readonly List<CommandSection> optional = new List<CommandSection>();
        readonly Action<List<ParsedSection>> action;

        public Command(Action<List<ParsedSection>> action)
            : this(null, action) {
        }
        public Command(CommandSection required, Action<List<ParsedSection>> action) {
            this.required = required;
            this.action = action;
        }

        public Command Option(CommandSection option) {
            optional.Add(option);
            return this;
        }

        public Action Parse(string[] args) {
            var result = new List<ParsedSection>();
            int offset = 0;            
            if(required != null) {
                ParsedSection parsed = ParseSection(required, args, ref offset);

                if(parsed == null)
                    return null;
                
                result.Add(parsed);
            }

            ParsedSection optionalParsed;
            List<CommandSection> found = new List<CommandSection>();
            do {
                optionalParsed = null;                
                foreach(var option in optional.Except(found)) {
                    optionalParsed = ParseSection(option, args, ref offset);
                    if(optionalParsed != null) {
                        result.Add(optionalParsed);
                        found.Add(option);
                        break;
                    }
                }
            }
            while(optionalParsed != null && offset < args.Length);

            if(result.Count > 0)
                return () => action(result);
            else
                return null;            
        }

        ParsedSection ParseSection(CommandSection section, string[] args, ref int offset) {
            ParsedSection parsed = section.Parse(args.Skip(offset).ToArray());
            if(parsed != null)
                offset += parsed.Length;                
            return parsed;
        }
    }
}