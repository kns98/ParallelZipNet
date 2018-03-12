using System;
using System.Collections.Generic;
using System.Linq;

namespace ParallelZipNet.Commands {
    public class CommandSection {
        readonly string name;
        readonly string[] keys;
        readonly string[] parameters;       

        public CommandSection(string name, string[] keys)
            : this(name, keys, new string[0]) {
        }
        public CommandSection(string name, string[] keys, string[] parameters) {
            if(name == null)
                throw new ArgumentNullException(nameof(name));
            if(keys == null)
                throw new ArgumentNullException(nameof(keys));
            if(parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            this.name = name;
            this.keys = keys;
            this.parameters = parameters;
        }

        public ParsedSection Parse(string[] args) {
            if(args == null)
                throw new ArgumentNullException(nameof(args));
            if(args.Length == 0)
                throw new ArgumentException("Can't be empty", nameof(args));

            if(args.Length < parameters.Length + 1)
                return null;

            if(!keys.Contains(args[0]))
                return null;
            
            var parsed = new ParsedSection(name);
            for(int i = 0; i < parameters.Length; i++)
                parsed.AddParameter(parameters[i], args[i + 1]);
            return parsed;
        }
    }
}