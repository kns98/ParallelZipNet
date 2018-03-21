using System.Collections.Generic;
using ParallelZipNet.Utils;

namespace ParallelZipNet.Threading {
        public class LockContext<T> {
        readonly IEnumerator<T> enumerator;

        public LockContext(IEnumerable<T> enumeration) {
            Guard.NotNull(enumeration, nameof(enumeration));
            
            enumerator = enumeration.GetEnumerator();
        }

        public IEnumerable<T> AsEnumerable() {
            while(true) {
                T result = default(T);
                bool success = false;                
                lock(enumerator) {
                    success = enumerator.MoveNext();
                    if(success)
                        result = enumerator.Current;
                }                
                if(success)
                    yield return result;                    
                else 
                    yield break;                    
            }
        }
    }
}