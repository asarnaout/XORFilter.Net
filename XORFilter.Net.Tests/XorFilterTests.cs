using System.Text;
using XORFilter.Net;

namespace XorFilter.Net.Tests
{
    public class XorFilterTests
    {
        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(50000)]
        [InlineData(100000)]
        [InlineData(5000000)]
        public void StringMembership(int size)
        {
            var guids = Enumerable.Range(0, size).Select(x => Guid.NewGuid().ToString()).ToArray();

            var values = guids.Select(Encoding.ASCII.GetBytes).ToArray();

            var filter = new XorFilter32();

            filter.Generate(values);

            for (var i = 0; i < guids.Length; i++)
            {
                Assert.True(filter.IsMember(Encoding.ASCII.GetBytes(guids[i])));
            }

            var randomValues = Enumerable.Range(0, size).Select(x => Guid.NewGuid().ToString()).ToArray();

            for (var i = 0; i < randomValues.Length; i++)
            {
                Assert.False(filter.IsMember(Encoding.ASCII.GetBytes(randomValues[i])));
            }
        }

        [Fact]
        public void SerializedObjectMembership()
        {
            var random = new Random();
            var dummyData = new PersonDummy[10];

            for (var i = 0; i < dummyData.Length; i++)
            {
                dummyData[i] = new PersonDummy
                {
                    Age = random.Next(20, 50),
                    Name = Guid.NewGuid().ToString()
                };
            }

            var filter = new XorFilter16();

            filter.Generate(dummyData.Select(x => x.Serialize()).ToArray());

            for (var i = 0; i < dummyData.Length; i++)
            {
                Assert.True(filter.IsMember(dummyData[i].Serialize()));
            }
        }

        private class PersonDummy
        {
            public required int Age { get; init; }

            public required string Name { get; init; } = default!;

            public byte[] Serialize()
            {
                using var m = new MemoryStream();
                using (var writer = new BinaryWriter(m))
                {
                    writer.Write(Age);
                    writer.Write(Name);
                }
                return m.ToArray();
            }
        }
    }
}