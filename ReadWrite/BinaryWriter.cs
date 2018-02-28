using System;
using System.IO;

namespace ParallelZipNet.ReadWrite {
    public interface IBinaryWriter {
        long Position { get; } 
        void WriteInt32(int value);
        void WriteBuffer(byte[] buffer);
        void WriteBuffer(byte[] buffer, long position);
    }

    public class BinaryFileWriter : IBinaryWriter, IDisposable {
        FileStream stream;

        public BinaryFileWriter(FileInfo info) {
            stream = info.OpenWrite();
        }

        public void Dispose() {
            if(stream != null) {
                stream.Dispose();
                stream = null;
            }            
        }

        long IBinaryWriter.Position => stream.Position;
        void IBinaryWriter.WriteInt32(int value) {
            stream.Write(BitConverter.GetBytes(value), 0, 4);
        }
        void IBinaryWriter.WriteBuffer(byte[] buffer) {
            stream.Write(buffer, 0, buffer.Length);
        }
        void IBinaryWriter.WriteBuffer(byte[] buffer, long position) {
            stream.Seek(position, SeekOrigin.Begin);
            ((IBinaryWriter)this).WriteBuffer(buffer);
        }        
    }
}