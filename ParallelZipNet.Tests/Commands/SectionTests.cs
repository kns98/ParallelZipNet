using System.Linq;
using FluentAssertions;
using ParallelZipNet.Commands;
using Xunit;

namespace ParallelZipNet.Tests.Commands {
    public class SectionTests {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        public void Length_Test(int keyCount) {
            var keys = Enumerable.Range(1, keyCount).Select(i => $"--testKey{i}").ToArray();
            var values = Enumerable.Range(1, keyCount).Select(i => $"--testValue{i}").ToArray();

            var section = new Section("TestName", keys, values);

            section.Length.Should().Be(keyCount + 1);
        }

        [Theory]        
        [InlineData("-k")]
        [InlineData("--full-key")]
        [InlineData("--full-key", "abc", " ")]
        public void TryParse_NoParams_Success_Test(params string[] args) {
            var section = new Section("Test", new[] { "--full-key", "-k" }, new string[0]);

            bool success = section.TryParse(args, out Option option);
            success.Should().BeTrue();
            option.Should().NotBeNull();
            option.Name.Should().Be("Test");
        }

        [Theory]
        [InlineData("--full-key", "1", "A")]
        [InlineData("--full-key", "1", "A", "abc", " ")]
        public void TryParse_WithParams_Success_Tetst(params string[] args) {
            var section = new Section("Test", new[] { "--full-key" }, new[] { "x", "y" });

            bool success = section.TryParse(args, out Option option);
            success.Should().BeTrue();
            option.Should().NotBeNull();
            option.Name.Should().Be("Test");
            option.GetIntegerParam("x").Should().Be(1);
            option.GetStringParam("y").Should().Be("A");
        }

        [Theory]
        [InlineData("--wrong-key", "1", "A")]
        [InlineData("--full-key", "1")]
        [InlineData("")]
        public void TryParse_Fail_Test(params string[] args) {
            var section = new Section("Test", new[] { "--full-key" }, new[] { "x", "y" });

            bool success = section.TryParse(args, out Option option);
            success.Should().BeFalse();
            option.Should().BeNull();            
        }
    }
}