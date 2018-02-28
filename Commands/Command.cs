using System;
using System.Linq;
using System.Collections.Generic;

namespace ParallelZipNet.Commands {
    public class Command {
        readonly string[] keys;
        readonly string[] parameters;
        readonly Action<IDictionary<string, string>> action;

        public Command(string[] keys, string[] parameters, Action<IDictionary<string, string>> action) {
            this.keys = keys;
            this.parameters = parameters;
            this.action = action;
        }

        public bool IsMatch(string key) => keys.Contains(key);

        public void Run(string[] args) {
            if(parameters.Length == args.Length) {
                var namedArgs = new Dictionary<string, string>();
                for(int i = 0; i < parameters.Length; i++)
                    namedArgs.Add(parameters[i], args[i]);
                action(namedArgs);
                return;
            }
            throw new Exception("Invalid command parameters");
        }
    }
}