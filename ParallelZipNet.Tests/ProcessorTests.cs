using System;
using System.IO;
using FluentAssertions;
using ParallelZipNet.Processor;
using Xunit;

namespace ParallelZipNet.Tests {
    public class ProcessorTests : IDisposable {
        const int chunkSize = 100;

        byte[] src;

        public ProcessorTests() {            
            src = new byte[10000];
            new Random().NextBytes(src);
        }

        public void Dispose() {
            src = null;
        }

        [Fact]
        public void CompressDecompress_Test() {
            var srcStream = new MemoryStream(src);
            var tempStream = new MemoryStream();
            var destStream = new MemoryStream();

            using(var srcReader = new BinaryReader(srcStream))
            using(var tempWriter = new BinaryWriter(tempStream))            
            using(var tempReader = new BinaryReader(tempStream))
            using(var destWriter = new BinaryWriter(destStream)) {
                Compressor.Run(srcReader, tempWriter, 1, chunkSize);

                tempWriter.Flush();
                tempWriter.Seek(0, SeekOrigin.Begin);

                Decompressor.Run(tempReader, destWriter, 1, chunkSize);

                byte[] dest = destStream.ToArray();

                dest.Should().Equal(src);
            }
        }

        [Fact]
        public void Decompress_InvalidData_Test() {
            var srcStream = new MemoryStream(src);
            var tempStream = new MemoryStream();

            using(var srcReader = new BinaryReader(srcStream))
            using(var tempWriter = new BinaryWriter(tempStream)) {
                Action act = () => Decompressor.Run(srcReader, tempWriter, 1, chunkSize);
                act.Should().Throw<InvalidDataException>();                
            }
        }
    }
} 