using System.Text;
using XORFilter.Net;

namespace XorFilter.Net.Tests
{
    public class XorFilter32Tests
    {
        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(50000)]
        public void Exists(int size)
        {
            var guids = Enumerable.Range(0, size).Select(x => Guid.NewGuid().ToString()).ToArray();

            var values = guids.Select(Encoding.ASCII.GetBytes).ToArray();

            var filter = new XorFilter32();

            filter.Generate(values);

            for (var i = 0; i < guids.Length; i++)
            {
                Assert.True(filter.IsMember(Encoding.ASCII.GetBytes(guids[i])));
            }

            Assert.False(filter.IsMember(Encoding.ASCII.GetBytes("some random string here that I know wont collide")));
        }
    }
}