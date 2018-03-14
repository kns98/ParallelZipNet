using System;
using System.Collections.Generic;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Commands {
    public class Option {
        readonly string name;
        readonly Dictionary<string, string> parameters = new Dictionary<string, string>();

        public string Name => name;

        public Option(string name) {
            Guard.NotNullOrWhiteSpace(name, nameof(name));

            this.name = name;
        }

        public void AddParameter(string key, string value) {
            Guard.NotNullOrWhiteSpace(key, nameof(key));
            Guard.NotNullOrWhiteSpace(value, nameof(value));

            parameters.Add(key, value);
        }

        public string GetStringParam(string key) {
            Guard.NotNullOrWhiteSpace(key, nameof(key));

            return parameters[key];
        }

        public int GetIntegerParam(string key) {
            Guard.NotNullOrWhiteSpace(key, nameof(key));
            
            return int.Parse(parameters[key]);
        }
    }
}