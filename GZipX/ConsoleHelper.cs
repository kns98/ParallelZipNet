using System;

namespace GZipX {
    static class ConsoleHelper {
        public static void LogException(Exception ex) {            
            var aggregateEx = ex as AggregateException;
            if(aggregateEx != null) {
                foreach(var innerEx in aggregateEx.InnerExceptions)
                    Console.WriteLine(innerEx.Message);
            }
            else
                Console.Write(ex.Message);
        }

        public static void LogChunk(int chunksProcessed, int chunkCount) {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write($"{chunksProcessed} of {chunkCount} chunks processed");
        }

        public static bool AskYesNoQuestion(string quiestion) {
            Console.WriteLine($"{quiestion} (Y/n)");
            return Console.ReadLine().TrimStart().TrimEnd().ToUpper() != "N";
        }

        public static void LogInvalidCommand() {
            Console.WriteLine(
@"The unknown command is specified.

Available commands:

GZipx compress <source> <destination>
GZipx decompress <source> <destination>"
            );
        }
    }
}
