using System;
using System.IO;
using FluentAssertions;
using ParallelZipNet.Processor;
using Xunit;
using FakeItEasy;
using ParallelZipNet.Logger;
using ParallelZipNet.ChunkLayer;

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

        byte[] CompressDecompress(Loggers loggersCompress = null, Loggers loggersDecompress = null) {
            var srcStream = new MemoryStream(src);
            var tempStream = new MemoryStream();
            var destStream = new MemoryStream();

            using(var srcReader = new BinaryReader(srcStream))
            using(var tempWriter = new BinaryWriter(tempStream))            
            using(var tempReader = new BinaryReader(tempStream))
            using(var destWriter = new BinaryWriter(destStream)) {
                Compressor.RunAsEnumerable(srcReader, tempWriter, 1, chunkSize, null, loggersCompress);

                tempWriter.Flush();
                tempWriter.Seek(0, SeekOrigin.Begin);

                Decompressor.RunAsEnumerable(tempReader, destWriter, 1, chunkSize, null, loggersDecompress);

                return destStream.ToArray();
            }            
        }

        [Fact]
        public void CompressDecompress_Test() {
            CompressDecompress().Should().Equal(src);
        }

        [Fact]
        public void Decompress_InvalidData_Test() {
            var srcStream = new MemoryStream(src);
            var destStream = new MemoryStream();

            using(var srcReader = new BinaryReader(srcStream))
            using(var destWriter = new BinaryWriter(destStream)) {
                Action act = () => Decompressor.RunAsEnumerable(srcReader, destWriter, 1, chunkSize);
                act.Should().Throw<InvalidDataException>();                
            }
        }

        [Fact]
        public void Compress_AggregateException_Test() {
            var fakeLogger = A.Fake<IChunkLogger>();

            A.CallTo(() => fakeLogger.LogChunk(A<string>._, A<Chunk>._)).Throws<InvalidOperationException>();

            var srcStream = new MemoryStream(src);
            var destStream = new MemoryStream();

            using(var srcReader = new BinaryReader(srcStream))
            using(var destWriter = new BinaryWriter(destStream)) {
                Action act = () => Compressor.RunAsEnumerable(srcReader, destWriter, 1, chunkSize, null, new Loggers { ChunkLogger = fakeLogger });
                act.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ChunkLogger_Test() {
            var fakeLoggerCompress = A.Fake<IChunkLogger>();
            var fakeLoggerDecompress = A.Fake<IChunkLogger>();

            CompressDecompress(
                new Loggers { ChunkLogger = fakeLoggerCompress }, 
                new Loggers { ChunkLogger =  fakeLoggerDecompress }
            );

            A.CallTo(() => fakeLoggerCompress.LogChunk(A<string>._, A<Chunk>._))
                .MustHaveHappened();
            A.CallTo(() => fakeLoggerDecompress.LogChunk(A<string>._, A<Chunk>._))
                .MustHaveHappened();                
        }

        [Fact]
        public void DefaultLogger_Test() {
            var fakeLoggerCompress = A.Fake<IDefaultLogger>();
            var fakeLoggerDecompress = A.Fake<IDefaultLogger>();

            CompressDecompress(
                new Loggers { DefaultLogger = fakeLoggerCompress }, 
                new Loggers { DefaultLogger =  fakeLoggerDecompress }
            );

            A.CallTo(() => fakeLoggerCompress.LogChunksProcessed(A<int>._, A<int>._))
                .MustHaveHappened();
            A.CallTo(() => fakeLoggerDecompress.LogChunksProcessed(A<int>._, A<int>._))
                .MustHaveHappened();
        }
    }
} 