using System.Collections.Generic;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Threading {
        public class LockContext<T> where T : class {
        readonly IEnumerator<T> enumerator;

        public LockContext(IEnumerable<T> enumeration) {
            Guard.NotNull(enumeration, nameof(enumeration));
            
            enumerator = enumeration.GetEnumerator();
        }

        public IEnumerable<T> AsEnumerable() {
            T result;
            while(true) {
                lock(enumerator) {
                    result = enumerator.MoveNext() ? enumerator.Current : null;
                }                
                if(result != null)
                    yield return result;                    
                else 
                    yield break;                    
            }
        }
    }
}