using System;
using System.Linq;
using System.Collections.Generic;

namespace ParallelZipNet {
    public class Command {
        readonly string[] keys;
        readonly string[] parameters;
        readonly Action<IDictionary<string, string>> action;

        public Command(string[] keys, Action<IDictionary<string, string>> action)
            : this(keys, new string[0], action) {
        }
        public Command(string[] keys, string[] parameters, Action<IDictionary<string, string>> action) {
            this.keys = keys;
            this.parameters = parameters;
            this.action = action;
        }

        public bool IsMatch(string key) => keys.Contains(key);

        public void Run(string[] args = null) {
            int length = args?.Length ?? 0;
            if(parameters.Length == length) {
                var namedArgs = new Dictionary<string, string>();
                for(int i = 0; i < length; i++)
                    namedArgs.Add(parameters[i], args[i]);
                action(namedArgs);
                return;
            }
            throw new Exception("Invalid command parameters");
        }
    }
}