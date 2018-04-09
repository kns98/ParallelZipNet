using ParallelZipNet.Commands;
using Xunit;
using FakeItEasy;
using System;
using System.Collections.Generic;
using FluentAssertions;

namespace ParallelZipNet.Tests.Commands {
    public class CommandProccessorTests {
        CommandProcessor processor = new CommandProcessor();
        Action<IEnumerable<Option>> operation1 = A.Fake<Action<IEnumerable<Option>>>();
        Action<IEnumerable<Option>> operation2 = A.Fake<Action<IEnumerable<Option>>>();

        public CommandProccessorTests() {
            processor.Register(operation1).Required("operation1");
            processor.Register(operation2).Required("operation2");
        }

        [Fact]
        public void Parse_Success_Test() {
            var action1 = processor.Parse(new[] { "operation1" });
            var action2 = processor.Parse(new[] { "operation2" });
            action1.Should().NotBeNull();
            action2.Should().NotBeNull();

            action1();
            action2();

            A.CallTo(() => operation1.Invoke(A<IEnumerable<Option>>._)).MustHaveHappened();
            A.CallTo(() => operation2.Invoke(A<IEnumerable<Option>>._)).MustHaveHappened();
        }

        [Fact]
        public void Parse_Fail_Test() {
            Action act = () => processor.Parse(new[] { "WRONG" });

            act.Should().Throw<UnknownCommandException>();
        }
    }
}
