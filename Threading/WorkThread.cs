using System;
using System.Threading;

namespace ParallelZipNet.Threading {
    class WorkThread<T> {
        readonly object itemLock = new object();
        readonly ReaderWriterLock errorLock = new ReaderWriterLock();
        readonly Thread thread;
        int keepRunning = 0;
        WorkItem<T> workItem;
        Exception error;

        Exception ErrorSafe {
            get {                
                errorLock.AcquireReaderLock(10000);
                try { 
                    return error;
                }
                finally {
                    errorLock.ReleaseReaderLock();
                }
            }
            set {
                errorLock.AcquireWriterLock(10000);
                try {
                    error = value;
                }
                finally {
                    errorLock.ReleaseWriterLock();
                }
            }
        }

        WorkItem<T> ItemSafe {
            get {
                lock(itemLock) {
                    return workItem;
                }
            }
            set {
                lock(itemLock) {
                    workItem = value;
                }
            }
        }

        public bool ErrorStatus { get { return ErrorSafe != null; } }
        public bool ReadyStatus { get { return !ErrorStatus && ItemSafe == null; } }

        public WorkThread() {
            thread = new Thread(Worker);
            thread.IsBackground = true;
            thread.Start();
        }

        public void Execute(WorkItem<T> workItem) {
            ItemSafe = workItem;
        }  

        public Exception HandleError() {
            Exception error = ErrorSafe;
            ErrorSafe = null;
            return error;
        }

        public void ShutDown() {
            Interlocked.Increment(ref keepRunning);
            thread.Join();
        }

        void Worker() {
            while(keepRunning == 0) {
                if(ErrorSafe == null) {
                    WorkItem<T> item = ItemSafe;
                    if(item != null) {
                        try {
                            item.Action.Invoke(item.WorkObject);
                        }
                        catch(Exception ex) {
                            ErrorSafe = ex;
                        }
                        finally {
                            ItemSafe = null;
                        }
                    }
                }
            }
        }
    }
}
