using System;
using System.Collections.Generic;
using System.Linq;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Commands {
    public class Section {
        readonly string name;
        readonly string[] keys;
        readonly string[] param;

        public int Length => param.Length + 1;

        public Section(string name, string[] keys, string[] param) {
            Guard.NotNullOrWhiteSpace(name, nameof(name));
            Guard.NotNullOrEmpty(keys, nameof(keys));
            Guard.NotNull(param, nameof(param));

            this.name = name;
            this.keys = keys;
            this.param = param;
        }

        public bool TryParse(string[] args, out Option option) {
            Guard.NotNull(args, nameof(args));

            if(args.Length >= param.Length + 1) {
                if(keys.Contains(args[0])) {
                    option = new Option(name);
                    for(int i = 0; i < param.Length; i++)
                        option.AddParameter(param[i], args[i + 1]);
                    return true;
                }
            }

            option = null;
            return false;
        }
    }
}