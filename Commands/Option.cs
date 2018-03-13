using System;
using System.Collections.Generic;

namespace ParallelZipNet.Commands {
    public class Option {
        readonly string name;
        readonly Dictionary<string, string> parameters = new Dictionary<string, string>();

        public string Name => name;

        public Option(string name) {
            if(name == null)
                throw new ArgumentNullException(nameof(name));
                
            this.name = name;
        }

        public void AddParameter(string key, string value) {
            parameters.Add(key, value);
        }

        public string GetStringParam(string key) {
            return parameters[key];
        }

        public int GetIntegerParam(string key) {
            return int.Parse(parameters[key]);
        }
    }
}