namespace ParallelZipNet.Commands {
    interface ICommand {
        bool CheckArgs(string[] args);
        int Execute(string[] args);
        void ShutDown();
    }
}
