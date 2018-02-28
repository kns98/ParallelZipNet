using System;
using System.IO;

namespace ParallelZipNet.ReadWrite {
    public interface IBinaryReader {
        long Position { get; } 
        long Length { get; }
        int ReadInt32();
        byte[] ReadBuffer(int length);
    }
    
    public class BinaryFileReader : IBinaryReader, IDisposable {
        FileStream stream;

        public BinaryFileReader(FileInfo info) {
            stream = info.OpenRead();            
        }

        public void Dispose() {
            if(stream != null) {
                stream.Dispose();
                stream = null;
            }            
        }

        long IBinaryReader.Position => stream.Position;
        long IBinaryReader.Length => stream.Length;
        int IBinaryReader.ReadInt32() {
            byte[] buffer = new byte[4];
            stream.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);            
        }
        byte[] IBinaryReader.ReadBuffer(int length) {
            byte[] buffer = new byte[length];
            stream.Read(buffer, 0, length);
            return buffer;            
        }
    }
}