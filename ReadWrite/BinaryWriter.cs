using System;
using System.IO;
using ParallelZipNet.Utils;

namespace ParallelZipNet.ReadWrite {
    public interface IBinaryWriter {
        long Position { get; } 
        void WriteInt32(int value);
        void WriteBuffer(byte[] buffer);
        void Seek(long position);
    }

    public class BinaryFileWriter : IBinaryWriter, IDisposable {
        BinaryWriter writer;

        public BinaryFileWriter(FileInfo info) {
            Guard.NotNull(info, nameof(info));
            
            writer = new BinaryWriter(info.OpenWrite());
        }

        public void Dispose() {
            if(writer != null) {
                writer.Dispose();
                writer = null;
            }            
        }

        long IBinaryWriter.Position => writer.BaseStream.Position;
        void IBinaryWriter.WriteInt32(int value) {
            writer.Write(value);
        }
        void IBinaryWriter.WriteBuffer(byte[] buffer) {
            Guard.NotNull(buffer, nameof(buffer));

            writer.Write(buffer);
        }
        void IBinaryWriter.Seek(long position) {
            Guard.NotNegative(position, nameof(position));

            writer.BaseStream.Seek(position, SeekOrigin.Begin);
        }
    }
}