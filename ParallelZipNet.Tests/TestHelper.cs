using System;
using System.Threading.Tasks;

namespace ParallelZipNet.Tests {
    public static class TestHelper {
        public static async Task WithTimeout(this Task task, int timeout) {
            Task delayTask = Task.Delay(timeout);
            Task firstToFinish = await Task.WhenAny(task, delayTask);
            
            if(firstToFinish == delayTask)
                throw new TimeoutException($"The task has been exceeded the timeout of {timeout} ms.");

            await task;
        } 
    }
}