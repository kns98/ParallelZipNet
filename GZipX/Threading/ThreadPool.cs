using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GZipX.Threading {
    class ThreadPool<T> {
        readonly object itemsLock = new object();
        readonly object threadsLock = new object();
        readonly Queue<WorkItem<T>> workItems = new Queue<WorkItem<T>>();
        readonly List<WorkThread<T>> workThreads = new List<WorkThread<T>>();
        readonly int maxThreads;
        readonly Thread schedulerThread;
        int keepRunning = 0;

        public bool Ready {
            get {
                lock(threadsLock) {
                    return workThreads.Count < maxThreads || ReadyThreadUnsafe != null;
                }
            }
        }

        WorkThread<T> ReadyThreadUnsafe { get { return workThreads.FirstOrDefault(wt => wt.ReadyStatus); } }        

        public ThreadPool(int maxThreads) {
            this.maxThreads = maxThreads;
            schedulerThread = new Thread(Scheduler);
            schedulerThread.Start();
        }

        public void EnqueueWork(T workObject, Action<T> action) {
            var workItem = new WorkItem<T>(workObject, action);
            lock(itemsLock) {
                workItems.Enqueue(workItem);
            }
        }

        public void ShutDown() {
            Interlocked.Increment(ref keepRunning);
            schedulerThread.Join();
        }

        public IEnumerable<Exception> HandleErrors() {
            lock(threadsLock) {
                return workThreads
                    .Where(wt => wt.ErrorStatus)
                    .Select(wt => wt.HandleError());
            }
        }

        void Scheduler() {
            while(keepRunning == 0) {
                bool hasWorks = false;
                lock(itemsLock) {
                    hasWorks = workItems.Count > 0;
                }
                if(hasWorks) {
                    WorkThread<T> readyThread = null;
                    lock(threadsLock) {
                        readyThread = ReadyThreadUnsafe;
                        if(readyThread == null && workThreads.Count < maxThreads) {
                            readyThread = new WorkThread<T>();
                            workThreads.Add(readyThread);
                        }
                    }
                    if(readyThread != null) {
                        lock(itemsLock) {
                            readyThread.Execute(workItems.Dequeue());
                        }
                    }
                }
                Thread.Yield();
            }
            CleanUp();
        }

        void CleanUp() {
            lock(threadsLock) {
                workThreads.ForEach(wt => wt.ShutDown());
                workThreads.Clear();
            }
            lock(itemsLock) {
                workItems.Clear();
            }
        }
    }
}
