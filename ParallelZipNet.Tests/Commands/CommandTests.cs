using System;
using System.Linq;
using System.Collections.Generic;
using FakeItEasy;
using FluentAssertions;
using ParallelZipNet.Commands;
using Xunit;

namespace ParallelZipNet.Tests.Commands {
    public class CommandTests {
        const string WRONG = "WRONG";

        [Theory]
        [InlineData("first")]
        [InlineData("first", WRONG)]
        [InlineData("first", "second")]
        [InlineData("first", WRONG, "second")]
        [InlineData("first", "second", WRONG)]
        [InlineData("first", WRONG, "third")]
        [InlineData("first", "third", WRONG)]
        [InlineData("first", "second", "third")]
        [InlineData("first", "third", "second")]
        [InlineData("first", "second", "third", WRONG)]
        [InlineData("first", "second", WRONG, "third")]
        [InlineData("first", WRONG, "second", "third")]
        public void NoParameters_WithRequiredSection_Success_Test(params string[] args) {
            Assert_NoParameters_Success(true, args);
        }

        [Theory]
        [InlineData("second")]
        [InlineData("second", WRONG)]
        [InlineData("third")]
        [InlineData("third", WRONG)]
        [InlineData("second", "third")]
        [InlineData("third", "second")]
        [InlineData("second", WRONG, "third")]
        [InlineData("second", "third", WRONG)]
        public void NoParameters_NoRequiredSection_Success_Test(params string[] args) {         
            Assert_NoParameters_Success(false, args);
        }        

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(WRONG)]
        [InlineData(WRONG, "first")]
        [InlineData("second")]
        [InlineData("third")]
        [InlineData("second", "third")]
        public void NoParameters_WithRequiredSection_Fail_Test(params string[] args) {
            Assert_NoParameters_Fail(true, args);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(WRONG)]
        [InlineData(WRONG, "second")]
        [InlineData(WRONG, "third")]
        [InlineData(WRONG, "second", "third")]
        public void NoParameters_NoRequiredSection_Fail_Test(params string[] args) {
            Assert_NoParameters_Fail(false, args);
        }

        [Fact]
        public void WithParameters_Test() {
            var fakeOperation = A.Fake<Action<IEnumerable<Option>>>();

            var command = new Command(fakeOperation)
                .Required("FIRST", new[] { "--firstKey" }, new[] { "firstParam" } )
                .Optional("SECOND", new[] { "--secondKey" }, new[] { "secondParam" });

            var args = new[] { "--firstKey", "A", "--secondKey", "B" };
            bool success = command.TryParse(args, out Action action);

            success.Should().BeTrue();            
            action.Should().NotBeNull();

            A.CallTo(() => fakeOperation.Invoke(A<IEnumerable<Option>>._)).Invokes((IEnumerable<Option> options) => {                
                var firstOption = options.Single(x => x.Name == "FIRST");
                var secondOption = options.Single(x => x.Name == "SECOND");
                firstOption.GetStringParam("firstParam").Should().Be("A");
                secondOption.GetStringParam("secondParam").Should().Be("B");
            });            

            action();

            A.CallTo(() => fakeOperation.Invoke(A<IEnumerable<Option>>._)).MustHaveHappened();            
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Any")]
        public void EmptyCommand_Test(params string[] args) {
            var fakeOperation = A.Fake<Action<IEnumerable<Option>>>();
            var command = new Command(fakeOperation);

            bool success = command.TryParse(args ?? new string[0], out Action action);

            success.Should().BeFalse();            
            action.Should().BeNull();            
        }

        void Assert_NoParameters_Success(bool withRequiredSection, string[] args) {
            var fakeOperation = A.Fake<Action<IEnumerable<Option>>>();

            var command = new Command(fakeOperation)
                .Optional("second")
                .Optional("third");

            if(withRequiredSection)
                command = command.Required("first");

            bool success = command.TryParse(args, out Action action);

            success.Should().BeTrue();            
            action.Should().NotBeNull();

            A.CallTo(() => fakeOperation.Invoke(A<IEnumerable<Option>>._)).Invokes((IEnumerable<Option> options) => {
                var expected = new List<string>();
                foreach(var arg in args) {
                    if(arg != "WRONG")
                        expected.Add(arg);
                    else
                        break;
                }                

                options.Select(x => x.Name).Should().Equal(expected);                
            });            

            action();

            A.CallTo(() => fakeOperation.Invoke(A<IEnumerable<Option>>._)).MustHaveHappened();
        }
        
        void Assert_NoParameters_Fail(bool withRequiredSection, string[] args) {
            var fakeOperation = A.Fake<Action<IEnumerable<Option>>>();

            var command = new Command(fakeOperation)
                .Optional("second")
                .Optional("third");

            if(withRequiredSection)
                command = command.Required("first");                

            bool success = command.TryParse(args ?? new string[0], out Action action);

            success.Should().BeFalse();            
            action.Should().BeNull();
        }
    }
}