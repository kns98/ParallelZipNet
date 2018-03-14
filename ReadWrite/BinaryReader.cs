using System;
using System.IO;
using ParallelZipNet.Utils;

namespace ParallelZipNet.ReadWrite {
    public interface IBinaryReader {
        long Position { get; } 
        long Length { get; }
        int ReadInt32();
        byte[] ReadBuffer(int length);
    }
    
    public class BinaryFileReader : IBinaryReader, IDisposable {
        BinaryReader reader;

        public BinaryFileReader(FileInfo info) {
            Guard.NotNull(info, nameof(info));

            reader = new BinaryReader(info.OpenRead());
        }

        public void Dispose() {
            if(reader != null) {
                reader.Dispose();
                reader = null;
            }            
        }

        long IBinaryReader.Position => reader.BaseStream.Position;
        long IBinaryReader.Length => reader.BaseStream.Length;
        int IBinaryReader.ReadInt32() {
            return reader.ReadInt32();
        }
        byte[] IBinaryReader.ReadBuffer(int length) {
            Guard.NotNegative(length, nameof(length));
            
            return reader.ReadBytes(length);
        }
    }
}