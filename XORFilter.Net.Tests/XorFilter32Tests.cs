using System.Text;
using XORFilter.Net;

namespace XorFilter.Net.Tests
{
    public class XorFilter32Tests
    {
        [Fact]
        public void Exists()
        {
            /*
             * TODO: This test is not guaranteed to pass due to the lack of ability to switch hash functions when peeling fails. 
             */

            var guids = Enumerable.Range(0, 10000).Select(x => Guid.NewGuid().ToString()).ToArray();

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