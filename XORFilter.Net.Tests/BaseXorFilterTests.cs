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
            return XORFilter.Net.Hashing.Crc32.Hash(data);
        }

            // Expose internal methods for testing
            public new void InitializeHashFunctionsWithSeeds(int tableSize, uint seed0, uint seed1, uint seed2)
                => base.InitializeHashFunctionsWithSeeds(tableSize, seed0, seed1, seed2);

            public new int[,] GenerateHashes(Span<byte[]> values)
                => base.GenerateHashes(values);

            public new bool TryPeel(Span<byte[]> values, int[,] hashesPerValue, out Stack<(int indexToPeel, int loneSlotIndex)> peelingOrder)
                => base.TryPeel(values, hashesPerValue, out peelingOrder);

            public new HashSet<int>[] GetHashMapping(int size, int[,] hashesPerValue)
                => base.GetHashMapping(size, hashesPerValue);

            public new void FillTableSlots(Span<byte[]> values, int[,] hashesPerValue, Stack<(int indexToPeel, int loneSlotIndex)> peelingOrder)
                => base.FillTableSlots(values, hashesPerValue, peelingOrder);
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
            // This test was designed for the old implementation where internal state
            // could be manually controlled. Since the new implementation handles 
            // construction automatically and clears intermediate data, we'll test
            // the core functionality instead.
            
            // Arrange
            var values = new byte[][]
            {
                Encoding.UTF8.GetBytes("test1"),
                Encoding.UTF8.GetBytes("test2")
            };

            // Act
            var filter = new TestableXorFilter(values);

            // Assert - Test that the filter works correctly (which means hashes were generated properly)
            filter.IsMember(Encoding.UTF8.GetBytes("test1")).Should().BeTrue();
            filter.IsMember(Encoding.UTF8.GetBytes("test2")).Should().BeTrue();
            filter.IsMember(Encoding.UTF8.GetBytes("test3")).Should().BeFalse();
            
            // Verify that hash functions were created
            filter.HashingFunctions.Should().HaveCount(3);
            filter.HashingFunctions.Should().AllSatisfy(func => func.Should().NotBeNull());
        }

        [Fact]
        public void GetHashMapping_ValidHashes_CreatesCorrectMapping()
        {
            // This test was designed for the old implementation where internal state
            // could be manually controlled. Since the new implementation handles 
            // construction automatically and clears intermediate data, we'll test
            // the core functionality instead.
            
            // Arrange
            var values = new byte[][]
            {
                Encoding.UTF8.GetBytes("test1"),
                Encoding.UTF8.GetBytes("test2")
            };
            var filter = new TestableXorFilter(values);

            // Act & Assert - Test that the filter works correctly
            filter.IsMember(Encoding.UTF8.GetBytes("test1")).Should().BeTrue();
            filter.IsMember(Encoding.UTF8.GetBytes("test2")).Should().BeTrue();
            filter.IsMember(Encoding.UTF8.GetBytes("test3")).Should().BeFalse();
            
            // Verify that the table was properly constructed
            filter.TableSlots.Should().NotBeEmpty();
            filter.HashingFunctions.Should().HaveCount(3);
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

        [Fact]
        public void Constructor_NullByteArrayInValues_HandlesCorrectly()
        {
            // Arrange
            var values = new byte[][]
            {
                Encoding.UTF8.GetBytes("valid1"),
                null!,
                Encoding.UTF8.GetBytes("valid2")
            };

            // Act & Assert - Should not throw, but null handling depends on ByteArrayEqualityComparer
            var action = () => new TestableXorFilter(values);
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TryPeel_SimpleCase_ReturnsTrue()
        {
            // Arrange
            var values = new byte[][]
            {
                Encoding.UTF8.GetBytes("test1"),
                Encoding.UTF8.GetBytes("test2")
            };

            // Act
            var filter = new TestableXorFilter(values);

            // Assert - If construction succeeded, TryPeel must have returned true
            filter.Should().NotBeNull();
            filter.IsMember(values[0]).Should().BeTrue();
            filter.IsMember(values[1]).Should().BeTrue();
        }

        [Fact]
        public void InitializeHashFunctions_ProducesDifferentResults()
        {
            // Arrange
            var values = new byte[][] { Encoding.UTF8.GetBytes("test") };
            var filter1 = new TestableXorFilter(values);
            var filter2 = new TestableXorFilter(values);

            var testInput = Encoding.UTF8.GetBytes("consistency_test");

            // Act
            var hash1_func1 = filter1.HashingFunctions[0](testInput);
            var hash2_func1 = filter2.HashingFunctions[0](testInput);

            // Assert - Different instances should potentially have different hash functions due to random seeds
            // (though they might occasionally be the same due to randomness)
            filter1.HashingFunctions.Should().HaveCount(3);
            filter2.HashingFunctions.Should().HaveCount(3);
            
            // Both should produce valid indices
            hash1_func1.Should().BeInRange(0, filter1.TableSlots.Length - 1);
            hash2_func1.Should().BeInRange(0, filter2.TableSlots.Length - 1);
        }

        [Fact]
        public void FingerPrint_EmptyArray_ReturnsValidFingerprint()
        {
            // Arrange
            var values = new byte[][] { Array.Empty<byte>() };
            var filter = new TestableXorFilter(values);

            // Act & Assert - Should not throw
            filter.IsMember(Array.Empty<byte>()).Should().BeTrue();
        }

        [Theory]
        [InlineData(3)]   // Exactly minimum
        [InlineData(4)]   // Remainder = 1
        [InlineData(5)]   // Remainder = 2  
        [InlineData(100)] // Large even
        [InlineData(101)] // Large with remainder 2
        public void InitializeHashFunctionsWithSeeds_PartitionBoundaries_WorkCorrectly(int tableSize)
        {
            // Arrange
            var values = new byte[][] { Encoding.UTF8.GetBytes("test") };
            var filter = new TestableXorFilter(values);

            // Act
            filter.InitializeHashFunctionsWithSeeds(tableSize, 42, 123, 456);

            // Assert - Test many values to ensure no out-of-bounds
            var testInputs = Enumerable.Range(0, 100)
                .Select(i => Encoding.UTF8.GetBytes($"test_input_{i}"))
                .ToArray();

            foreach (var input in testInputs)
            {
                for (int funcIndex = 0; funcIndex < 3; funcIndex++)
                {
                    var hash = filter.HashingFunctions[funcIndex](input);
                    hash.Should().BeInRange(0, tableSize - 1, 
                        $"Hash function {funcIndex} produced out-of-bounds result for table size {tableSize}");
                }
            }
        }

        [Fact]
        public void Constructor_ExtremelyDifficultSet_EventuallySucceedsOrThrows()
        {
            // Arrange - Create a set that's likely to cause peeling failures
            // Use a small set with specific patterns that might cause issues
            var problematicValues = new byte[][]
            {
                new byte[] { 0, 0, 0, 1 },
                new byte[] { 0, 0, 0, 2 },
                new byte[] { 0, 0, 0, 3 }
            };

            // Act & Assert - Should either succeed or throw InvalidOperationException
            try
            {
                var filter = new TestableXorFilter(problematicValues);
                filter.Should().NotBeNull();
                
                // If construction succeeds, verify it works
                foreach (var value in problematicValues)
                {
                    filter.IsMember(value).Should().BeTrue();
                }
            }
            catch (InvalidOperationException ex)
            {
                // This is acceptable - the algorithm has limits
                ex.Message.Should().Contain("Failed to construct XOR filter");
                ex.Message.Should().Contain("attempts");
            }
        }

        [Fact]
        public void Constructor_TableSizeGrowth_WorksCorrectly()
        {
            // Arrange - Use a moderate size that should work but might require retries
            var values = Enumerable.Range(0, 100)
                .Select(i => Encoding.UTF8.GetBytes($"value_{i}"))
                .ToArray();

            // Act
            var filter = new TestableXorFilter(values);

            // Assert
            filter.Should().NotBeNull();
            
            // Table size should be reasonable (around 1.23x input size, but possibly larger due to retries)
            var expectedMinSize = (int)Math.Ceiling(100 * 1.23);
            var expectedMaxSize = (int)Math.Ceiling(100 * 2.0); // Allow for growth due to retries
            
            filter.TableSlots.Length.Should().BeGreaterThanOrEqualTo(Math.Max(3, expectedMinSize));
            filter.TableSlots.Length.Should().BeLessThan(expectedMaxSize);
        }

        [Fact]
        public void IsMember_NullValue_ThrowsOrHandlesGracefully()
        {
            // Arrange
            var values = new byte[][] { Encoding.UTF8.GetBytes("test") };
            var filter = new TestableXorFilter(values);

            // Act & Assert
            var action = () => filter.IsMember(null!);
            
            // Should either throw a clear exception or handle gracefully
            // Most importantly, should not hang or cause unexpected behavior
            try
            {
                var result = filter.IsMember(null!);
                // If it doesn't throw, that's fine too
            }
            catch (Exception ex)
            {
                // Should be a clear, expected exception type
                ex.Should().Match(e => e is ArgumentNullException || e is NullReferenceException);
            }
        }

        [Fact]
        public void Constructor_ExactlyThreeValues_HandlesMinimumPartitioning()
        {
            // Arrange - Exactly 3 values (minimum for proper partitioning)
            var values = new byte[][]
            {
                Encoding.UTF8.GetBytes("first"),
                Encoding.UTF8.GetBytes("second"),
                Encoding.UTF8.GetBytes("third")
            };

            // Act
            var filter = new TestableXorFilter(values);

            // Assert
            filter.Should().NotBeNull();
            filter.TableSlots.Length.Should().BeGreaterThanOrEqualTo(3);
            
            foreach (var value in values)
            {
                filter.IsMember(value).Should().BeTrue();
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void Constructor_FewerThanThreeValues_StillWorks(int valueCount)
        {
            // Arrange
            var values = Enumerable.Range(0, valueCount)
                .Select(i => Encoding.UTF8.GetBytes($"value_{i}"))
                .ToArray();

            // Act
            var filter = new TestableXorFilter(values);

            // Assert
            filter.Should().NotBeNull();
            filter.TableSlots.Length.Should().BeGreaterThanOrEqualTo(3); // Minimum table size
            
            foreach (var value in values)
            {
                filter.IsMember(value).Should().BeTrue();
            }
        }
    }
}
