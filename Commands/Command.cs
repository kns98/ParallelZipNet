using System;
using System.Linq;
using System.Collections.Generic;

namespace ParallelZipNet.Commands {
    public class Command {
        readonly Action<IEnumerable<Option>> action;        
        readonly List<Section> optionalSections = new List<Section>();
        Section requiredSection;

        public Action<IEnumerable<Option>> Action => action;
        public Section RequiredSection => requiredSection;
        public IEnumerable<Section> OptionalSections => optionalSections;

        public Command(Action<IEnumerable<Option>> action) {
            this.action = action;
        }

        public Command Required(string key) {
            return Required(key, new[] { key }, new string[0]);
        }

        public Command Required(string name, string[] keys, string[] param) {
            requiredSection = new Section(name, keys, param);
            return this;
        }

        public Command Optional(string key) {
            return Optional(key, new[] { key }, new string[0]);
        }

        public Command Optional(string name, string[] keys, string[] param) {
            var section = new Section(name, keys, param);
            optionalSections.Add(section);
            return this;
        }
    }
}