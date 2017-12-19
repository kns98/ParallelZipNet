using System;
using System.IO;

namespace GZipX {
    class StreamWrapper : IDisposable {
        FileStream srcStream;
        FileStream destStream;

        public long BytesToRead { get { return srcStream.Length - srcStream.Position; } }
        public long TotalBytesToRead { get { return srcStream.Length; } }
        
        public StreamWrapper(FileInfo srcInfo, FileInfo destInfo) {
            srcStream = srcInfo.OpenRead();
            destStream = destInfo.OpenWrite();
        }

        public void Dispose() {
            if(srcStream != null) {
                srcStream.Dispose();
                srcStream = null;
            }
            if(destStream != null) {
                destStream.Dispose();
                destStream = null;
            }
        }

        public void WriteInt32(int value) {
            destStream.Write(BitConverter.GetBytes(value), 0, 4);
        }

        public void WriteBuffer(byte[] buffer) {
            destStream.Write(buffer, 0, buffer.Length);
        }

        public void WriteBuffer(byte[] buffer, long position) {
            destStream.Seek(position, SeekOrigin.Begin);
            WriteBuffer(buffer);
        }

        public int ReadInt32() {
            byte[] buffer = new byte[4];
            srcStream.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public byte[] ReadBuffer(int length) {
            byte[] buffer = new byte[length];
            srcStream.Read(buffer, 0, length);
            return buffer;
        }
    }
}
