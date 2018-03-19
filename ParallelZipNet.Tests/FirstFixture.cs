using ParallelZipNet.Commands;
using Xunit;
using FluentAssertions;

namespace ParallelZipNet.Tests
{
    public class FirstFixture {
        [Fact]
        public void FirstTest() {
            var section = new Section("test", new string[] { "test" }, new string[0]);
            section.TryParse(new string[0], out Option option).Should().BeTrue();
        }
    }
}
