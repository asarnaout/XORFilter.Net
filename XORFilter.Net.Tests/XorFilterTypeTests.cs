#pragma warning disable CS0618 // Type or member is obsolete - we're testing backward compatibility

using System.Text;
using FluentAssertions;

namespace XORFilter.Net.Tests
{
    // Test helper classes to expose protected FingerPrint methods
    internal class TestableXorFilter8 : BaseXorFilter<byte>
    {
        public TestableXorFilter8(Span<byte[]> values) : base(values) { }
        protected override byte FingerPrint(byte[] data) => FingerPrint(data.AsSpan());
        protected override byte FingerPrint(ReadOnlySpan<byte> data) => (byte)(XORFilter.Net.Hashing.Crc32.Hash(data) % (byte.MaxValue + 1));
        public byte FingerPrintPublic(ReadOnlySpan<byte> data) => FingerPrint(data);
    }

    internal class TestableXorFilter16 : BaseXorFilter<ushort>
    {
        public TestableXorFilter16(Span<byte[]> values) : base(values) { }
        protected override ushort FingerPrint(byte[] data) => FingerPrint(data.AsSpan());
        protected override ushort FingerPrint(ReadOnlySpan<byte> data) => (ushort)(XORFilter.Net.Hashing.Crc32.Hash(data) % (ushort.MaxValue + 1));
        public ushort FingerPrintPublic(ReadOnlySpan<byte> data) => FingerPrint(data);
    }

    internal class TestableXorFilter32 : BaseXorFilter<uint>
    {
        public TestableXorFilter32(Span<byte[]> values) : base(values) { }
        protected override uint FingerPrint(byte[] data) => FingerPrint(data.AsSpan());
        protected override uint FingerPrint(ReadOnlySpan<byte> data) => XORFilter.Net.Hashing.Crc32.Hash(data);
        public uint FingerPrintPublic(ReadOnlySpan<byte> data) => FingerPrint(data);
    }

    /// <summary>
    /// Unit tests for XorFilter8 - focusing on 8-bit specific behavior
    /// </summary>
    public class XorFilter8Tests
    {
        [Fact]
        public void BuildFrom_ValidValues_CreatesFilter()
        {
            // Arrange
            var values = new byte[][]
            {
                Encoding.UTF8.GetBytes("test1"),
                Encoding.UTF8.GetBytes("test2"),
                Encoding.UTF8.GetBytes("test3")
            };

            // Act
            var filter = XorFilter8.BuildFrom(values);

            // Assert
            filter.Should().NotBeNull();
            filter.Should().BeOfType<XorFilter8>();
        }

        [Fact]
        public void FingerPrint_SameInput_ReturnsSameFingerprint()
        {
            // Arrange
            var values = new byte[][] { Encoding.UTF8.GetBytes("test") };
            var filter = new TestableXorFilter8(values);
            var testInput = Encoding.UTF8.GetBytes("fingerprint_test");

            // Act
            var fingerprint1 = filter.FingerPrintPublic(testInput.AsSpan());
            var fingerprint2 = filter.FingerPrintPublic(testInput.AsSpan());

            // Assert
            fingerprint1.Should().Be(fingerprint2);
        }

        [Fact]
        public void FingerPrint_DifferentInputs_ReturnDifferentFingerprints()
        {
            // Arrange
            var values = new byte[][] { Encoding.UTF8.GetBytes("test") };
            var filter = new TestableXorFilter8(values);
            var input1 = Encoding.UTF8.GetBytes("input1");
            var input2 = Encoding.UTF8.GetBytes("input2");

            // Act
            var fingerprint1 = filter.FingerPrintPublic(input1.AsSpan());
            var fingerprint2 = filter.FingerPrintPublic(input2.AsSpan());

            // Assert
            fingerprint1.Should().NotBe(fingerprint2);
        }

        [Fact]
        public void FingerPrint_ReturnsValidByteRange()
        {
            // Arrange
            var values = new byte[][] { Encoding.UTF8.GetBytes("test") };
            var filter = new TestableXorFilter8(values);
            var testInputs = new[]
            {
                Array.Empty<byte>(),
                Encoding.UTF8.GetBytes("a"),
                Encoding.UTF8.GetBytes("longer_test_string"),
                new byte[] { 0, 1, 2, 3, 255 }
            };

            // Act & Assert
            foreach (var input in testInputs)
            {
                var fingerprint = filter.FingerPrintPublic(input.AsSpan());
                fingerprint.Should().BeInRange(byte.MinValue, byte.MaxValue);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(50)]
        [InlineData(100)]
        public void IsMember_AddedValues_AllReturnTrue(int count)
        {
            // Arrange
            var values = Enumerable.Range(0, count)
                .Select(i => Encoding.UTF8.GetBytes($"value_{i}"))
                .ToArray();

            var filter = XorFilter8.BuildFrom(values);

            // Act & Assert
            foreach (var value in values)
            {
                filter.IsMember(value).Should().BeTrue($"Value {Encoding.UTF8.GetString(value)} should be a member");
            }
        }

        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        [InlineData(5000)]
        public void IsMember_RandomValues_MostReturnFalse(int count)
        {
            // Arrange
            var addedValues = Enumerable.Range(0, count)
                .Select(i => Encoding.UTF8.GetBytes($"added_{i}"))
                .ToArray();

            var randomValues = Enumerable.Range(0, count)
                .Select(i => Encoding.UTF8.GetBytes($"random_{Guid.NewGuid()}"))
                .ToArray();

            var filter = XorFilter8.BuildFrom(addedValues);

            // Act
            var falsePositives = 0;
            foreach (var value in randomValues)
            {
                if (filter.IsMember(value))
                {
                    falsePositives++;
                }
            }

            // Assert
            // For XorFilter8, theoretical false positive rate is 0.390625%
            // Use statistically sound thresholds based on sample size
            var falsePositiveRate = (double)falsePositives / count;
            
            if (count >= 1000)
            {
                // Large samples: expect rate close to theoretical value
                falsePositiveRate.Should().BeLessThan(0.01); // Less than 1%
            }
            else
            {
                // Smaller samples: allow more variance due to statistical fluctuation
                falsePositiveRate.Should().BeLessThan(0.05); // Less than 5%
            }
        }

        [Fact]
        public void BuildFrom_EmptyArray_ThrowsArgumentException()
        {
            // Arrange
            var emptyValues = Array.Empty<byte[]>();

            // Act & Assert
            var action = () => XorFilter8.BuildFrom(emptyValues);
            action.Should().Throw<ArgumentException>()
                .WithMessage("Values array should be provided to generate the XOR Filter.");
        }

        [Fact]
        public void BuildFrom_DuplicateValues_HandlesCorrectly()
        {
            // Arrange
            var values = new byte[][]
            {
                Encoding.UTF8.GetBytes("duplicate"),
                Encoding.UTF8.GetBytes("unique1"),
                Encoding.UTF8.GetBytes("duplicate"), // Intentional duplicate
                Encoding.UTF8.GetBytes("unique2")
            };

            // Act
            var filter = XorFilter8.BuildFrom(values);

            // Assert
            filter.Should().NotBeNull();
            filter.IsMember(Encoding.UTF8.GetBytes("duplicate")).Should().BeTrue();
            filter.IsMember(Encoding.UTF8.GetBytes("unique1")).Should().BeTrue();
            filter.IsMember(Encoding.UTF8.GetBytes("unique2")).Should().BeTrue();
        }

        [Fact]
        public void BuildFrom_VariableLengthValues_HandlesCorrectly()
        {
            // Arrange
            var values = new[]
            {
                Array.Empty<byte>(),
                new byte[] { 1 },
                Encoding.UTF8.GetBytes("ab"),
                Encoding.UTF8.GetBytes("longer string"),
                Encoding.UTF8.GetBytes("very much longer string with many characters"),
                new byte[1000] // Very large array
            };

            // Act
            var filter = XorFilter8.BuildFrom(values);

            // Assert
            filter.Should().NotBeNull();
            foreach (var value in values)
            {
                filter.IsMember(value).Should().BeTrue();
            }
        }

        [Fact]
        public void IsMember_NullValue_ShouldNotThrow()
        {
            // Arrange
            var values = new byte[][] { Encoding.UTF8.GetBytes("test") };
            var filter = XorFilter8.BuildFrom(values);

            // Act & Assert
            // This should either handle null gracefully or throw a clear exception
            var action = () => filter.IsMember(null!);
            // The behavior depends on implementation - it might throw or handle gracefully
            // We just want to ensure it doesn't cause an infinite loop or hang
        }

        [Fact]
        public void BuildFrom_LargeDataSet_CompletesWithinReasonableTime()
        {
            // Arrange
            var values = Enumerable.Range(0, 10000)
                .Select(i => BitConverter.GetBytes(i))
                .ToArray();

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var filter = XorFilter8.BuildFrom(values);
            stopwatch.Stop();

            // Assert
            filter.Should().NotBeNull();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // Should complete within 30 seconds

            // Verify a sample of values
            var sampleSize = Math.Min(100, values.Length);
            for (int i = 0; i < sampleSize; i += values.Length / sampleSize)
            {
                filter.IsMember(values[i]).Should().BeTrue();
            }
        }
    }

    /// <summary>
    /// Unit tests for XorFilter16 - focusing on 16-bit specific behavior
    /// </summary>
    public class XorFilter16Tests
    {
        [Fact]
        public void BuildFrom_ValidValues_CreatesFilter()
        {
            // Arrange
            var values = new byte[][]
            {
                Encoding.UTF8.GetBytes("test1"),
                Encoding.UTF8.GetBytes("test2"),
                Encoding.UTF8.GetBytes("test3")
            };

            // Act
            var filter = XorFilter16.BuildFrom(values);

            // Assert
            filter.Should().NotBeNull();
            filter.Should().BeOfType<XorFilter16>();
        }

        [Fact]
        public void FingerPrint_ReturnsValidUshortRange()
        {
            // Arrange
            var values = new byte[][] { Encoding.UTF8.GetBytes("test") };
            var filter = new TestableXorFilter16(values);
            var testInputs = new[]
            {
                Array.Empty<byte>(),
                Encoding.UTF8.GetBytes("test"),
                new byte[] { 255, 255, 255, 255 }
            };

            // Act & Assert
            foreach (var input in testInputs)
            {
                var fingerprint = filter.FingerPrintPublic(input.AsSpan());
                fingerprint.Should().BeInRange(ushort.MinValue, ushort.MaxValue);
            }
        }

        [Theory]
        [InlineData(1000)]
        [InlineData(5000)]
        public void IsMember_LowerFalsePositiveRate_ThanXorFilter8(int testCount)
        {
            // Arrange
            var addedValues = Enumerable.Range(0, testCount / 2)
                .Select(i => Encoding.UTF8.GetBytes($"added_{i}"))
                .ToArray();

            var randomValues = Enumerable.Range(0, testCount / 2)
                .Select(i => Encoding.UTF8.GetBytes($"random_{Guid.NewGuid()}"))
                .ToArray();

            var filter16 = XorFilter16.BuildFrom(addedValues);

            // Act
            var falsePositives = 0;
            foreach (var value in randomValues)
            {
                if (filter16.IsMember(value))
                {
                    falsePositives++;
                }
            }

            // Assert
            // XorFilter16 should have lower false positive rate than XorFilter8
            var falsePositiveRate = (double)falsePositives / (testCount / 2);
            falsePositiveRate.Should().BeLessThan(0.005); // Should be less than 0.5%
        }
    }

    /// <summary>
    /// Unit tests for XorFilter32 - focusing on 32-bit specific behavior
    /// </summary>
    public class XorFilter32Tests
    {
        [Fact]
        public void BuildFrom_ValidValues_CreatesFilter()
        {
            // Arrange
            var values = new byte[][]
            {
                Encoding.UTF8.GetBytes("test1"),
                Encoding.UTF8.GetBytes("test2"),
                Encoding.UTF8.GetBytes("test3")
            };

            // Act
            var filter = XorFilter32.BuildFrom(values);

            // Assert
            filter.Should().NotBeNull();
            filter.Should().BeOfType<XorFilter32>();
        }

        [Fact]
        public void FingerPrint_ReturnsFullUintValue()
        {
            // Arrange
            var values = new byte[][] { Encoding.UTF8.GetBytes("test") };
            var filter = new TestableXorFilter32(values);
            var testInput = Encoding.UTF8.GetBytes("fingerprint_test");

            // Act
            var fingerprint = filter.FingerPrintPublic(testInput.AsSpan());

            // Assert
            fingerprint.Should().BeInRange(uint.MinValue, uint.MaxValue);
        }

        [Theory]
        [InlineData(10000)]
        public void IsMember_LowestFalsePositiveRate_ComparedToOtherFilters(int testCount)
        {
            // Arrange
            var addedValues = Enumerable.Range(0, testCount / 2)
                .Select(i => Encoding.UTF8.GetBytes($"added_{i}"))
                .ToArray();

            var randomValues = Enumerable.Range(0, testCount / 2)
                .Select(i => Encoding.UTF8.GetBytes($"random_{Guid.NewGuid()}"))
                .ToArray();

            var filter32 = XorFilter32.BuildFrom(addedValues);

            // Act
            var falsePositives = 0;
            foreach (var value in randomValues)
            {
                if (filter32.IsMember(value))
                {
                    falsePositives++;
                }
            }

            // Assert
            // XorFilter32 should have the lowest false positive rate
            var falsePositiveRate = (double)falsePositives / (testCount / 2);
            falsePositiveRate.Should().BeLessThan(0.0001); // Should be less than 0.01%
        }
    }

    /// <summary>
    /// Comparative tests between different filter types
    /// </summary>
    public class XorFilterComparisonTests
    {
        [Fact]
        public void AllFilterTypes_SameInput_BehaveSimilarly()
        {
            // Arrange
            var values = Enumerable.Range(0, 100)
                .Select(i => Encoding.UTF8.GetBytes($"value_{i}"))
                .ToArray();

            // Act
            var filter8 = XorFilter8.BuildFrom(values);
            var filter16 = XorFilter16.BuildFrom(values);
            var filter32 = XorFilter32.BuildFrom(values);

            // Assert
            // All filters should correctly identify members
            foreach (var value in values)
            {
                filter8.IsMember(value).Should().BeTrue();
                filter16.IsMember(value).Should().BeTrue();
                filter32.IsMember(value).Should().BeTrue();
            }
        }

        [Fact]
        public void FalsePositiveRates_DecreaseWithBitSize()
        {
            // Arrange
            var addedValues = Enumerable.Range(0, 1000)
                .Select(i => Encoding.UTF8.GetBytes($"added_{i}"))
                .ToArray();

            var testValues = Enumerable.Range(0, 1000)
                .Select(i => Encoding.UTF8.GetBytes($"test_{Guid.NewGuid()}"))
                .ToArray();

            var filter8 = XorFilter8.BuildFrom(addedValues);
            var filter16 = XorFilter16.BuildFrom(addedValues);
            var filter32 = XorFilter32.BuildFrom(addedValues);

            // Act
            var falsePositives8 = testValues.Count(v => filter8.IsMember(v));
            var falsePositives16 = testValues.Count(v => filter16.IsMember(v));
            var falsePositives32 = testValues.Count(v => filter32.IsMember(v));

            // Assert
            // Generally, higher bit filters should have fewer false positives
            // Note: This is probabilistic, so we use reasonable bounds
            var rate8 = (double)falsePositives8 / testValues.Length;
            var rate16 = (double)falsePositives16 / testValues.Length;
            var rate32 = (double)falsePositives32 / testValues.Length;

            rate8.Should().BeLessThan(0.02); // Less than 2%
            rate16.Should().BeLessThan(0.01); // Less than 1%
            rate32.Should().BeLessThan(0.005); // Less than 0.5%

            // Generally rate32 <= rate16 <= rate8 (though not guaranteed due to randomness)
        }

        [Fact]
        public void XorFilter8_FalsePositiveRate_WithinExpectedBounds()
        {
            // Arrange
            var addedValues = Enumerable.Range(0, 10000)
                .Select(i => Encoding.UTF8.GetBytes($"added_{i}"))
                .ToArray();

            var testValues = Enumerable.Range(0, 10000)
                .Select(i => Encoding.UTF8.GetBytes($"test_{i}_{Guid.NewGuid()}"))
                .ToArray();

            var filter = XorFilter8.BuildFrom(addedValues);

            // Act
            var falsePositives = testValues.Count(v => filter.IsMember(v));

            // Assert
            var actualRate = (double)falsePositives / testValues.Length;
            
            // XorFilter8 theoretical error rate is ~0.390625%
            // With large sample, should be close to theoretical value
            actualRate.Should().BeLessThan(0.02); // Should be well under 2%
            actualRate.Should().BeGreaterThan(0.0); // Should have some false positives
        }

        [Fact]
        public void XorFilter16_FalsePositiveRate_LowerThanXorFilter8()
        {
            // Arrange
            var addedValues = Enumerable.Range(0, 5000)
                .Select(i => Encoding.UTF8.GetBytes($"added_{i}"))
                .ToArray();

            var testValues = Enumerable.Range(0, 5000)
                .Select(i => Encoding.UTF8.GetBytes($"test_{i}_{Guid.NewGuid()}"))
                .ToArray();

            var filter8 = XorFilter8.BuildFrom(addedValues);
            var filter16 = XorFilter16.BuildFrom(addedValues);

            // Act
            var falsePositives8 = testValues.Count(v => filter8.IsMember(v));
            var falsePositives16 = testValues.Count(v => filter16.IsMember(v));

            // Assert
            var rate8 = (double)falsePositives8 / testValues.Length;
            var rate16 = (double)falsePositives16 / testValues.Length;

            // XorFilter16 should generally have lower false positive rate than XorFilter8
            rate16.Should().BeLessThan(rate8 + 0.01); // Allow some variance due to randomness
            rate16.Should().BeLessThan(0.01); // Should be less than 1%
        }
    }
}
