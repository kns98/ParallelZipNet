using System;
using System.Linq;
using System.Collections.Generic;
using ParallelZipNet.Utils;
using Guards;

namespace ParallelZipNet.Commands {
    public class Command {        
        readonly Action<IEnumerable<Option>> action;        
        readonly List<Section> optionalSections = new List<Section>();
        Section requiredSection;

        public Command(Action<IEnumerable<Option>> action) {
            Guard.NotNull(action, nameof(action));

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

         public bool TryParse(string[] args, out Action action) {
            Guard.NotNull(args, nameof(args));

            var options = new List<Option>();
            int offset = 0;            

            if(requiredSection != null) {                
                if(requiredSection.TryParse(args, out Option required)) {
                    options.Add(required);
                    offset += requiredSection.Length;                    
                }
                else {
                    action = null;
                    return false;                    
                }
            }

            Option optional;
            List<Section> parsedSections = new List<Section>();
            do {
                optional = null;                
                string[] sectionArgs = args.Skip(offset).ToArray();
                foreach(var optionalSection in optionalSections.Except(parsedSections)) {
                    if(optionalSection.TryParse(sectionArgs, out optional)) {
                        options.Add(optional);
                        offset += optionalSection.Length;
                        parsedSections.Add(optionalSection);
                        break;
                    }
                }
            }
            while(optional != null && offset < args.Length);
            
            if(options.Count > 0) {                 
                action = () => this.action(options);
                return true;
            }
            else {
                action = null;
                return false;            
            }
        }
    }
}