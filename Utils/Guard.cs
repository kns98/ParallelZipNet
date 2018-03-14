using System;

namespace ParallelZipNet.Utils {
    public static class Guard {
        public static void NotNull(object argument, string name) {
            if(argument == null)
                throw new ArgumentNullException(name);
        }

        public static void NotNullOrWhiteSpace(string argument, string name) {
            if(string.IsNullOrWhiteSpace(argument))
                throw new ArgumentException($"String {name} must be not null or white space", name);
        }

        public static void NotNullOrEmpty(Array argument, string name) {
            NotNull(argument, name);
            if(argument.Length == 0)
                throw new ArgumentException($"Array {name} must contain at least one element", name);
        }

        public static void NotNegative(int argument, string name) {
            if(argument < 0)
                throw new ArgumentException($"Number {name} must be not negative", name);
        }

        public static void NotNegative(long argument, string name) {
            if(argument < 0)
                throw new ArgumentException($"Long Number {name} must be not negative", name);
        }        

        public static void NotZeroOrNegative(int argument, string name) {
            if(argument <= 0)
                throw new ArgumentException($"Number {name} must be not zero or negative", name);
        }
    }
}