using System;
using System.Collections.Generic;

namespace ParallelZipNet.Threading {
    public class ErrorHandler {
        readonly List<Exception> errors = new List<Exception>();

        public void Handle(Exception error) {
            lock(errors) {
                errors.Add(error);
            }
        }

        public void ThrowIfFailed() {
            if(errors.Count > 0)
                throw new AggregateException(errors);
        }
    }
}