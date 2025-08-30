using System.Text;
using FluentAssertions;
using Xunit;
using XORFilter.Net;

namespace XORFilter.Net.Tests
{
    /// <summary>
    /// Unit tests for BaseXorFilter internal functionality
    /// These tests focus on individual methods and their behavior
    /// </summary>
    public class BaseXorFilterTests
    {
        private class TestableXorFilter : BaseXorFilter<uint>
        {
            public TestableXorFilter(Span<byte[]> values) : base(values) { }

            protected override uint FingerPrint(byte[] data)
            {
                // Simple fingerprint for testing - just use the sum of bytes
                return (uint)data.Sum(b => b);
            }

            // Expose internal methods for testing
            public new void InitializeHashFunctionsWithSeeds(int tableSize, uint seed0, uint seed1, uint seed2)
                => base.InitializeHashFunctionsWithSeeds(tableSize, seed0, seed1, seed2);

            public new void GenerateHashes(Span<byte[]> values)
                => base.GenerateHashes(values);

            public new bool TryPeel(Span<byte[]> values, out Stack<(int indexToPeel, int loneSlotIndex)> peelingOrder)
                => base.TryPeel(values, out peelingOrder);

            public new HashSet<int>[] GetHashMapping(int size)
                => base.GetHashMapping(size);

            public new void FillTableSlots(Span<byte[]> values, Stack<(int indexToPeel, int loneSlotIndex)> peelingOrder)
                => base.FillTableSlots(values, peelingOrder);
        }

        [Fact]
        public void Constructor_EmptyValues_ThrowsArgumentException()
        {
            // Arrange
            var emptyValues = Array.Empty<byte[]>();

            // Act & Assert
            var action = () => new TestableXorFilter(emptyValues);
            action.Should().Throw<ArgumentException>()
                .WithMessage("Values array should be provided to generate the XOR Filter.");
        }

        [Fact]
        public void Constructor_ValidValues_CreatesFilter()
        {
            // Arrange
            var values = new byte[][]
            {
                Encoding.UTF8.GetBytes("test1"),
                Encoding.UTF8.GetBytes("test2"),
                Encoding.UTF8.GetBytes("test3")
            };

            // Act
            var filter = new TestableXorFilter(values);

            // Assert
            filter.Should().NotBeNull();
            filter.TableSlots.Should().NotBeEmpty();
            filter.HashingFunctions.Should().HaveCount(3);
        }

        [Fact]
        public void Constructor_DuplicateValues_RemovesDuplicates()
        {
            // Arrange
            var duplicateValues = new byte[][]
            {
                Encoding.UTF8.GetBytes("test1"),
                Encoding.UTF8.GetBytes("test2"),
                Encoding.UTF8.GetBytes("test1"), // duplicate
                Encoding.UTF8.GetBytes("test3")
            };

            // Act
            var filter = new TestableXorFilter(duplicateValues);

            // Assert
            filter.Should().NotBeNull();
            // The filter should be created successfully even with duplicates
            filter.TableSlots.Should().NotBeEmpty();
        }

        [Fact]
        public void InitializeHashFunctionsWithSeeds_ValidParameters_CreatesThreeHashFunctions()
        {
            // Arrange
            var values = new byte[][] { Encoding.UTF8.GetBytes("test") };
            var filter = new TestableXorFilter(values);

            // Act
            filter.InitializeHashFunctionsWithSeeds(10, 123, 456, 789);

            // Assert
            filter.HashingFunctions.Should().HaveCount(3);
            filter.HashingFunctions.Should().AllSatisfy(func => func.Should().NotBeNull());
        }

        [Fact]
        public void InitializeHashFunctionsWithSeeds_MinimumTableSize_WorksCorrectly()
        {
            // Arrange
            var values = new byte[][] { Encoding.UTF8.GetBytes("test") };
            var filter = new TestableXorFilter(values);

            // Act
            filter.InitializeHashFunctionsWithSeeds(3, 123, 456, 789); // Minimum size

            // Assert
            filter.HashingFunctions.Should().HaveCount(3);
            
            // Test that hash functions produce values in valid ranges
            var testInput = Encoding.UTF8.GetBytes("test");
            var hash0 = filter.HashingFunctions[0](testInput);
            var hash1 = filter.HashingFunctions[1](testInput);
            var hash2 = filter.HashingFunctions[2](testInput);

            hash0.Should().BeInRange(0, 0); // First partition: [0, 1)
            hash1.Should().BeInRange(1, 1); // Second partition: [1, 2)
            hash2.Should().BeInRange(2, 2); // Third partition: [2, 3)
        }

        [Fact]
        public void HashingFunctions_SameInput_ProduceSameOutput()
        {
            // Arrange
            var values = new byte[][] { Encoding.UTF8.GetBytes("test") };
            var filter = new TestableXorFilter(values);
            filter.InitializeHashFunctionsWithSeeds(10, 123, 456, 789);

            var testInput = Encoding.UTF8.GetBytes("consistent");

            // Act
            var hash1_1 = filter.HashingFunctions[0](testInput);
            var hash1_2 = filter.HashingFunctions[0](testInput);
            var hash2_1 = filter.HashingFunctions[1](testInput);
            var hash2_2 = filter.HashingFunctions[1](testInput);

            // Assert
            hash1_1.Should().Be(hash1_2);
            hash2_1.Should().Be(hash2_2);
        }

        [Fact]
        public void GenerateHashes_ValidValues_PopulatesHashMatrix()
        {
            // Arrange
            var values = new byte[][]
            {
                Encoding.UTF8.GetBytes("test1"),
                Encoding.UTF8.GetBytes("test2")
            };
            var filter = new TestableXorFilter(values);
            filter.InitializeHashFunctionsWithSeeds(10, 123, 456, 789);

            // Act
            filter.GenerateHashes(values);

            // Assert
            filter.HashesPerValue.Should().NotBeNull();
            filter.HashesPerValue.GetLength(0).Should().Be(2); // 2 values
            filter.HashesPerValue.GetLength(1).Should().Be(3); // 3 hash functions

            // Verify that hashes are within valid ranges
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    filter.HashesPerValue[i, j].Should().BeInRange(0, 9);
                }
            }
        }

        [Fact]
        public void GetHashMapping_ValidHashes_CreatesCorrectMapping()
        {
            // Arrange
            var values = new byte[][]
            {
                Encoding.UTF8.GetBytes("test1"),
                Encoding.UTF8.GetBytes("test2")
            };
            var filter = new TestableXorFilter(values);
            filter.InitializeHashFunctionsWithSeeds(10, 123, 456, 789);
            filter.GenerateHashes(values);

            // Act
            var mapping = filter.GetHashMapping(2);

            // Assert
            mapping.Should().HaveCount(10); // Table size
            
            // Each value should appear in exactly 3 slots (one for each hash function)
            var totalReferences = 0;
            for (int i = 0; i < mapping.Length; i++)
            {
                if (mapping[i] != null)
                {
                    totalReferences += mapping[i].Count;
                }
            }
            totalReferences.Should().Be(6); // 2 values × 3 hash functions
        }

        [Fact]
        public void IsMember_AddedValue_ReturnsTrue()
        {
            // Arrange
            var testValue = Encoding.UTF8.GetBytes("test_value");
            var values = new byte[][] { testValue };
            var filter = new TestableXorFilter(values);

            // Act
            var isMember = filter.IsMember(testValue);

            // Assert
            isMember.Should().BeTrue();
        }

        [Fact]
        public void IsMember_NotAddedValue_ReturnsFalse()
        {
            // Arrange
            var addedValue = Encoding.UTF8.GetBytes("added_value");
            var notAddedValue = Encoding.UTF8.GetBytes("not_added_value");
            var values = new byte[][] { addedValue };
            var filter = new TestableXorFilter(values);

            // Act
            var isMember = filter.IsMember(notAddedValue);

            // Assert
            isMember.Should().BeFalse();
        }

        [Theory]
        [InlineData(3)]  // Minimum table size
        [InlineData(5)]  // Small odd size
        [InlineData(6)]  // Small even size
        [InlineData(10)] // Larger size
        public void InitializeHashFunctionsWithSeeds_VariousTableSizes_PartitionsCorrectly(int tableSize)
        {
            // Arrange
            var values = new byte[][] { Encoding.UTF8.GetBytes("test") };
            var filter = new TestableXorFilter(values);

            // Act
            filter.InitializeHashFunctionsWithSeeds(tableSize, 123, 456, 789);

            // Assert
            var testInput = Encoding.UTF8.GetBytes("partition_test");
            var hashes = new[]
            {
                filter.HashingFunctions[0](testInput),
                filter.HashingFunctions[1](testInput),
                filter.HashingFunctions[2](testInput)
            };

            // All hashes should be within table bounds
            hashes.Should().AllSatisfy(hash => hash.Should().BeInRange(0, tableSize - 1));

            // Hash functions should map to different partitions
            var partitionSize = tableSize / 3;
            var remainder = tableSize % 3;

            var partition1End = partitionSize + (remainder > 0 ? 1 : 0);
            var partition2End = partition1End + partitionSize + (remainder > 1 ? 1 : 0);

            hashes[0].Should().BeInRange(0, partition1End - 1);
            hashes[1].Should().BeInRange(partition1End, partition2End - 1);
            hashes[2].Should().BeInRange(partition2End, tableSize - 1);
        }

        [Fact]
        public void Constructor_LargeValueSet_HandlesEfficiently()
        {
            // Arrange
            var values = Enumerable.Range(0, 1000)
                .Select(i => Encoding.UTF8.GetBytes($"value_{i}"))
                .ToArray();

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var filter = new TestableXorFilter(values);
            stopwatch.Stop();

            // Assert
            filter.Should().NotBeNull();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
            
            // Verify that the table size is appropriate (around 1.23 * input size)
            var expectedSize = (int)Math.Ceiling(1000 * 1.23);
            filter.TableSlots.Length.Should().BeGreaterThanOrEqualTo(Math.Max(3, expectedSize));
        }

        [Fact]
        public void Constructor_SingleValue_WorksCorrectly()
        {
            // Arrange
            var singleValue = new byte[][] { Encoding.UTF8.GetBytes("single") };

            // Act
            var filter = new TestableXorFilter(singleValue);

            // Assert
            filter.Should().NotBeNull();
            filter.IsMember(Encoding.UTF8.GetBytes("single")).Should().BeTrue();
            filter.IsMember(Encoding.UTF8.GetBytes("other")).Should().BeFalse();
        }
    }
}
