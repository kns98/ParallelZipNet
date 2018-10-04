using System;
using System.Collections.Generic;
using System.Linq;
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

        public T GetFlags<T>(string key) {
            string[] enumNames = parameters[key].Split("_");
            string enumString = string.Join(",", enumNames);

            return (T)Enum.Parse(typeof(T), enumString, ignoreCase: true);
        }

        public int GetIntegerParam(string key, int minConstraint = int.MinValue, int maxContstaint = int.MaxValue) {
            Guard.NotNullOrWhiteSpace(key, nameof(key));
            Guard.NotMinGreaterThanMax(minConstraint, maxContstaint, $"[{nameof(minConstraint)}, {nameof(maxContstaint)}]");
            
            int param = int.Parse(parameters[key]);
            
            if(param < minConstraint)
                return minConstraint;
            else if(param > maxContstaint)            
                return maxContstaint;
            else
                return param;
        }
    }
}