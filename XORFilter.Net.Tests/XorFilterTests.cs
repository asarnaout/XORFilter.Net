using System.Text;
using XORFilter.Net;
using System.Buffers.Binary;

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

            var filter = XorFilter32.BuildFrom(values);

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

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(50000)]
        [InlineData(100000)]
        [InlineData(5000000)]
        public void IntMembership(int size)
        {
            var random = new Random();

            var numbers = Enumerable.Range(0, size).Select(x => random.Next()).ToArray();

            var values = numbers.Select(ToByteArray).ToArray();

            var filter = XorFilter8.BuildFrom(values);

            for (var i = 0; i < numbers.Length; i++)
            {
                Assert.True(filter.IsMember(ToByteArray(numbers[i])));
            }

            static byte[] ToByteArray(int val)
            {
                var byteArray = new byte[sizeof(int)];
                BinaryPrimitives.WriteInt32LittleEndian(byteArray, val);
                return byteArray;
            }
        }

        [Fact]
        public void IntMembershipWithDuplicates()
        {
            var size = 1000000;
            var random = new Random();

            var numbers = Enumerable.Range(0, size).Select(x => random.Next()).ToArray();

            numbers[100] = 3000;
            numbers[1000] = 3000;
            numbers[10000] = 3000;

            var values = numbers.Select(ToByteArray).ToArray();

            var filter = XorFilter8.BuildFrom(values);

            for (var i = 0; i < numbers.Length; i++)
            {
                Assert.True(filter.IsMember(ToByteArray(numbers[i])));
            }

            static byte[] ToByteArray(int val)
            {
                var byteArray = new byte[sizeof(int)];
                BinaryPrimitives.WriteInt32LittleEndian(byteArray, val);
                return byteArray;
            }
        }

        [Fact]
        public void SerializedObjectMembership()
        {
            var random = new Random();
            var dummyData = new PersonDummy[1000000];

            for (var i = 0; i < dummyData.Length; i++)
            {
                dummyData[i] = new PersonDummy
                {
                    Age = random.Next(20, 50),
                    Name = Guid.NewGuid().ToString()
                };
            }

            var filter = XorFilter16.BuildFrom(dummyData.Select(x => x.Serialize()).ToArray());

            for (var i = 0; i < dummyData.Length; i++)
            {
                Assert.True(filter.IsMember(dummyData[i].Serialize()));
            }
        }

        [Fact]
        public void SerializedObjectMembershipWithDuplicates()
        {
            var random = new Random();
            var dummyData = new PersonDummy[1000000];

            for (var i = 0; i < dummyData.Length; i++)
            {
                dummyData[i] = new PersonDummy
                {
                    Age = random.Next(20, 50),
                    Name = Guid.NewGuid().ToString()
                };
            }

            dummyData[100] = dummyData[1000];

            var filter = XorFilter16.BuildFrom(dummyData.Select(x => x.Serialize()).ToArray());

            for (var i = 0; i < dummyData.Length; i++)
            {
                Assert.True(filter.IsMember(dummyData[i].Serialize()));
            }
        }

        [Fact]
        public void VariableLengthStringsMembership()
        {
            var maliciousUrls = new List<string>
            {
                "getscammednow.com",
                "malicious-software-is-cool.net",
                "getrichquickfrfr.org",
                "legitssncheck.com",
                "totallylegitcreditcardnumberlookup.com",
                "getmalwarenow.com"
            };

            var encodedValues = maliciousUrls.Select(Encoding.ASCII.GetBytes).ToArray();
            
            var filter = XorFilter32.BuildFrom(encodedValues);

            var isMaliciousUrl = filter.IsMember(Encoding.ASCII.GetBytes("getrichquickfrfr.org"));

            Assert.True(isMaliciousUrl);
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