using System;
using ParallelZipNet.Commands;
using Xunit;

namespace ParallelZipNet.Tests {
    public class FirstFixture {
        [Fact]
        public void FirstTest() {
            var section = new Section("test", new string[] { "test" }, new string[0]);
            Assert.False(section.TryParse(new string[0], out Option option));
        }
    }
}
