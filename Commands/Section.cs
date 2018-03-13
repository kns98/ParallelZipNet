using System;
using System.Collections.Generic;
using System.Linq;

namespace ParallelZipNet.Commands {
    public class Section {
        readonly string name;
        readonly string[] keys;
        readonly string[] param;

        public int Length => param.Length + 1;

        public Section(string name, string[] keys, string[] param) {
            this.name = name;
            this.keys = keys;
            this.param = param;
        }

        public Option Parse(string[] args) {
            if(args == null)
                throw new ArgumentNullException(nameof(args));

            if(args.Length < param.Length + 1)
                return null;

            if(!keys.Contains(args[0]))
                return null;
            
            var parsed = new Option(name);
            for(int i = 0; i < param.Length; i++)
                parsed.AddParameter(param[i], args[i + 1]);
            return parsed;
        }
    }
}