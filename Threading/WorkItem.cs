using System;

namespace ParallelZipNet.Threading {
    class WorkItem<T> {
        public T WorkObject { get; private set; }
        public Action<T> Action { get; private set; }

        public WorkItem(T workObject, Action<T> action) {
            WorkObject = workObject;
            Action = action;
        }
    }
}
