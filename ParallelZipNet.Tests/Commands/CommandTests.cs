using System;
using System.Collections.Generic;
using FakeItEasy;
using FluentAssertions;
using ParallelZipNet.Commands;
using Xunit;

namespace ParallelZipNet.Tests.Commands {
    public class CommandTests {
        [Fact]
        public void First_Test() {
            var fakeOperation = A.Fake<Action<IEnumerable<Option>>>();

            var command = new Command(fakeOperation);
            command
                .Required("req")
                .Optional("OPT1", new[] { "--opt1" }, new[] { "value1" })
                .Optional("--opt2" );

            bool success = command.TryParse(new[] { "req", "--opt1", "A", "--opt2" }, out Action action);

            success.Should().BeTrue();
            action.Should().NotBeNull();

            A.CallTo(() => fakeOperation.Invoke(A<IEnumerable<Option>>._)).Invokes((IEnumerable<Option> options) => {
                options.Should().HaveCount(3);
            });            

            action();

            A.CallTo(() => fakeOperation.Invoke(A<IEnumerable<Option>>._)).MustHaveHappened();
        }
    }
}