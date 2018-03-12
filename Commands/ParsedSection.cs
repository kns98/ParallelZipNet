using System;
using System.Collections.Generic;

namespace ParallelZipNet.Commands {
    public class ParsedSection {
        readonly string name;
        readonly Dictionary<string, string> parameters = new Dictionary<string, string>();

        public string Name => name;
        public int Length => parameters.Count + 1;

        public ParsedSection(string name) {
            if(name == null)
                throw new ArgumentNullException(nameof(name));
                
            this.name = name;
        }

        public void AddParameter(string key, string value) {
            parameters.Add(key, value);
        }

        public string GetStringParameter<T>(string key) {
            return parameters[key];
        }

        public int GetIntegerParameter(string key) {
            return int.Parse(parameters[key]);
        }
    }
}