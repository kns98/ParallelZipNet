using ParallelZipNet.Commands;
using Xunit;
using FakeItEasy;
using System;
using System.Collections.Generic;

namespace ParallelZipNet.Tests.Commands {
    public class CommandProccessorTests {
        CommandProcessor processor = new CommandProcessor();
        Action<IEnumerable<Option>> fakeOperation1 = A.Fake<Action<IEnumerable<Option>>>();
        Action<IEnumerable<Option>> fakeOperation2 = A.Fake<Action<IEnumerable<Option>>>();

        public CommandProccessorTests() {
            processor.Register(fakeOperation1);
            processor.Register(fakeOperation2);
        }

        [Fact]
        public void Parse_Success_Test() {
        }        
    }
}
