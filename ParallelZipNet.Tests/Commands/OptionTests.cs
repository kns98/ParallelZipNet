using System;
using System.Collections.Generic;
using FluentAssertions;
using ParallelZipNet.Commands;
using Xunit;

namespace ParallelZipNet.Tests.Commands {
    public class OptionTests {
        [Theory]
        [InlineData("First")]
        [InlineData("Second")]
        public void Name_Test(string name) {
            var option = new Option(name);
            option.Name.Should().Be(name);
        }

        [Theory]
        [InlineData("First", "A")]
        [InlineData("Second", "B")]
        public void StringParam_Test(string paramKey, string paramValue) {
            var option = new Option("Test");
            option.AddParameter(paramKey, paramValue);

            option.GetStringParam(paramKey).Should().Be(paramValue);
            option.Invoking(opt => opt.GetIntegerParam(paramKey)).Should().Throw<FormatException>();
        }

        [Theory]
        [InlineData("First", 10)]
        [InlineData("Second", -1000)]
        public void IntegerParam_Test(string paramKey, int paramValue) {
            var option = new Option("Test");
            option.AddParameter(paramKey, paramValue.ToString());

            option.GetIntegerParam(paramKey).Should().Be(paramValue);
            option.GetStringParam(paramKey).Should().Be(paramValue.ToString());
        }

        [Theory]
        [InlineData(-1000, 0)]
        [InlineData(-1, 0)]
        [InlineData(0, 0)]
        [InlineData(5, 5)]
        [InlineData(10, 10)]
        [InlineData(11, 10)]
        [InlineData(1000, 10)]
        public void IntegerParam_OutOfConstraints_0_10_Test(int probe, int expected) {
            var option = new Option("Test");
            option.AddParameter("First", probe.ToString());

            option.GetIntegerParam("First", 0, 10).Should().Be(expected);
        }

        [Fact]
        public void NotExistParam_Test() {
            var option = new Option("Test");

            option.Invoking(opt => opt.GetStringParam("Any")).Should().Throw<KeyNotFoundException>();
            option.Invoking(opt => opt.GetIntegerParam("Any")).Should().Throw<KeyNotFoundException>();
        }
    }
}