using System.Text;
using FluentAssertions;
using Xunit;
using XORFilter.Net;

namespace XORFilter.Net.Tests
{
    /// <summary>
    /// Edge case and stress tests to ensure robustness
    /// </summary>
    public class EdgeCaseTests
    {
        [Fact]
        public void BuildFrom_SingleByteValues_HandlesCorrectly()
        {
            // Arrange
            var values = new byte[][]
            {
                new byte[] { 0 },
                new byte[] { 1 },
                new byte[] { 255 },
                new byte[] { 127 }
            };

            // Act
            var filter = XorFilter32.BuildFrom(values);

            // Assert
            filter.Should().NotBeNull();
            foreach (var value in values)
            {
                filter.IsMember(value).Should().BeTrue();
            }
        }

        [Fact]
        public void BuildFrom_EmptyByteArrays_HandlesCorrectly()
        {
            // Arrange
            var values = new byte[][]
            {
                Array.Empty<byte>(),
                Encoding.UTF8.GetBytes("not_empty")
            };

            // Act
            var filter = XorFilter32.BuildFrom(values);

            // Assert
            filter.Should().NotBeNull();
            filter.IsMember(Array.Empty<byte>()).Should().BeTrue();
            filter.IsMember(Encoding.UTF8.GetBytes("not_empty")).Should().BeTrue();
        }

        [Fact]
        public void BuildFrom_OnlyEmptyArrays_HandlesCorrectly()
        {
            // Arrange
            var values = new byte[][]
            {
                Array.Empty<byte>(),
                Array.Empty<byte>(), // Duplicate empty arrays
                Array.Empty<byte>()
            };

            // Act
            var filter = XorFilter32.BuildFrom(values);

            // Assert
            filter.Should().NotBeNull();
            filter.IsMember(Array.Empty<byte>()).Should().BeTrue();
        }

        [Fact]
        public void BuildFrom_VeryLargeByteArrays_HandlesCorrectly()
        {
            // Arrange
            var largeArray1 = new byte[100000];
            var largeArray2 = new byte[100000];
            
            // Fill with different patterns to ensure they're different
            for (int i = 0; i < largeArray1.Length; i++)
            {
                largeArray1[i] = (byte)(i % 256);
                largeArray2[i] = (byte)((i + 1) % 256);
            }

            var values = new byte[][] { largeArray1, largeArray2 };

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var filter = XorFilter32.BuildFrom(values);
            stopwatch.Stop();

            // Assert
            filter.Should().NotBeNull();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // Should complete within 10 seconds
            filter.IsMember(largeArray1).Should().BeTrue();
            filter.IsMember(largeArray2).Should().BeTrue();
        }

        [Fact]
        public void BuildFrom_AllIdenticalValues_HandlesCorrectly()
        {
            // Arrange - All arrays have the same content
            var identicalValue = Encoding.UTF8.GetBytes("identical");
            var values = new byte[][]
            {
                identicalValue,
                identicalValue,
                identicalValue,
                identicalValue
            };

            // Act
            var filter = XorFilter32.BuildFrom(values);

            // Assert
            filter.Should().NotBeNull();
            filter.IsMember(identicalValue).Should().BeTrue();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)] // Minimum for partitioning
        [InlineData(4)]
        [InlineData(5)]
        public void BuildFrom_SmallValueCounts_HandlesCorrectly(int valueCount)
        {
            // Arrange
            var values = Enumerable.Range(0, valueCount)
                .Select(i => Encoding.UTF8.GetBytes($"value_{i}"))
                .ToArray();

            // Act
            var filter = XorFilter32.BuildFrom(values);

            // Assert
            filter.Should().NotBeNull();
            foreach (var value in values)
            {
                filter.IsMember(value).Should().BeTrue();
            }
        }

        [Fact]
        public void BuildFrom_BinaryData_HandlesCorrectly()
        {
            // Arrange - Binary data that might cause issues
            var values = new byte[][]
            {
                new byte[] { 0x00, 0x00, 0x00, 0x00 },
                new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
                new byte[] { 0xAA, 0x55, 0xAA, 0x55 },
                new byte[] { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80 }
            };

            // Act
            var filter = XorFilter32.BuildFrom(values);

            // Assert
            filter.Should().NotBeNull();
            foreach (var value in values)
            {
                filter.IsMember(value).Should().BeTrue();
            }
        }

        [Fact]
        public void BuildFrom_SequentialValues_HandlesCorrectly()
        {
            // Arrange - Sequential byte patterns that might reveal issues
            var values = Enumerable.Range(0, 256)
                .Select(i => new byte[] { (byte)i })
                .ToArray();

            // Act
            var filter = XorFilter32.BuildFrom(values);

            // Assert
            filter.Should().NotBeNull();
            foreach (var value in values)
            {
                filter.IsMember(value).Should().BeTrue();
            }
        }

        [Fact]
        public void IsMember_ThreadSafety_ConcurrentReads()
        {
            // Arrange
            var values = Enumerable.Range(0, 100)
                .Select(i => Encoding.UTF8.GetBytes($"value_{i}"))
                .ToArray();

            var filter = XorFilter32.BuildFrom(values);
            var testValue = values[50];
            var errors = new List<Exception>();

            // Act - Concurrent reads
            var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        var result = filter.IsMember(testValue);
                        if (!result)
                        {
                            throw new InvalidOperationException("Expected value not found");
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (errors)
                    {
                        errors.Add(ex);
                    }
                }
            })).ToArray();

            Task.WaitAll(tasks);

            // Assert
            errors.Should().BeEmpty("Concurrent reads should not cause errors");
        }

        [Fact]
        public void Constructor_RepeatedConstruction_IsConsistent()
        {
            // Arrange
            var values = Enumerable.Range(0, 50)
                .Select(i => Encoding.UTF8.GetBytes($"value_{i}"))
                .ToArray();

            // Act - Build multiple filters with same input
            var filters = Enumerable.Range(0, 5)
                .Select(_ => XorFilter32.BuildFrom(values))
                .ToArray();

            // Assert - All filters should work correctly
            foreach (var filter in filters)
            {
                foreach (var value in values)
                {
                    filter.IsMember(value).Should().BeTrue();
                }
            }
        }

        [Fact]
        public void BuildFrom_MaximalDuplication_HandlesCorrectly()
        {
            // Arrange - Same value repeated many times
            var baseValue = Encoding.UTF8.GetBytes("repeated");
            var values = Enumerable.Range(0, 1000)
                .Select(_ => baseValue)
                .ToArray();

            // Act
            var filter = XorFilter32.BuildFrom(values);

            // Assert
            filter.Should().NotBeNull();
            filter.IsMember(baseValue).Should().BeTrue();
        }
    }

    /// <summary>
    /// Performance and stress tests
    /// </summary>
    public class StressTests
    {
        [Theory]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(50000)]
        public void BuildFrom_LargeDataSets_CompletesWithinTimeLimit(int size)
        {
            // Arrange
            var values = Enumerable.Range(0, size)
                .Select(i => BitConverter.GetBytes(i))
                .ToArray();

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var filter = XorFilter32.BuildFrom(values);
            stopwatch.Stop();

            // Assert
            filter.Should().NotBeNull();
            // Time limit scales with input size - should be roughly linear
            var maxTimeMs = size * 5; // 5ms per 1000 items maximum
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxTimeMs);

            // Verify a sample of the data
            var sampleSize = Math.Min(100, size);
            for (int i = 0; i < sampleSize; i += Math.Max(1, size / sampleSize))
            {
                filter.IsMember(BitConverter.GetBytes(i)).Should().BeTrue();
            }
        }

        [Fact]
        public void MemoryUsage_LargeFilter_RemainsReasonable()
        {
            // Arrange
            var values = Enumerable.Range(0, 100000)
                .Select(i => BitConverter.GetBytes(i))
                .ToArray();

            // Act
            var filter = XorFilter32.BuildFrom(values);

            // Assert
            filter.Should().NotBeNull();
            
            // Verify the filter works correctly (more important than memory usage)
            filter.IsMember(BitConverter.GetBytes(0)).Should().BeTrue();
            filter.IsMember(BitConverter.GetBytes(99999)).Should().BeTrue();
            filter.IsMember(BitConverter.GetBytes(100000)).Should().BeFalse(); // Not in the set
            
            // Basic sanity check: filter table should be reasonable size
            // For 100,000 items with 1.23x factor, expect ~123,000 slots
            // This is testing actual implementation behavior rather than unreliable GC measurements
            var tableSize = filter.TableSlots.Length;
            tableSize.Should().BeGreaterThan(100000); // Must be larger than input
            tableSize.Should().BeLessThan(200000); // But not excessively large
            
            // Performance check: queries should be fast
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                filter.IsMember(BitConverter.GetBytes(i));
            }
            stopwatch.Stop();
            
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(50, 
                "1000 membership queries should complete quickly");
        }

        [Fact]
        public void QueryPerformance_ManyQueries_CompletesQuickly()
        {
            // Arrange
            var values = Enumerable.Range(0, 10000)
                .Select(i => Encoding.UTF8.GetBytes($"value_{i}"))
                .ToArray();

            var filter = XorFilter32.BuildFrom(values);
            var queryValues = values.Take(1000).ToArray();

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            foreach (var value in queryValues)
            {
                filter.IsMember(value);
            }
            
            stopwatch.Stop();

            // Assert
            // 1000 queries should complete very quickly (under 100ms)
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
        }

        [Fact]
        public void Construction_DoesNotHangIndefinitely()
        {
            // Arrange - Use a pattern that might cause construction issues
            var problematicValues = new List<byte[]>();
            
            // Add values that might cause hash collisions or peeling issues
            for (int i = 0; i < 1000; i++)
            {
                problematicValues.Add(BitConverter.GetBytes(i));
                problematicValues.Add(BitConverter.GetBytes(i * 2)); // Related values
                problematicValues.Add(BitConverter.GetBytes(i ^ 0xAAAA)); // XOR pattern
            }

            // Act with timeout
            XorFilter32? filter = null;
            var completed = false;
            
            var task = Task.Run(() =>
            {
                filter = XorFilter32.BuildFrom(problematicValues.ToArray());
                completed = true;
            });

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(60)); // 60 second timeout
            var completedTask = Task.WhenAny(task, timeoutTask).Result;

            // Assert
            completedTask.Should().Be(task, "Construction should complete before timeout");
            completed.Should().BeTrue("Construction should complete");
            filter.Should().NotBeNull();
        }

        [Fact]
        public void BuildFrom_HighlyColliding_HandlesGracefully()
        {
            // Arrange - Create values likely to cause hash collisions
            var collidingValues = new List<byte[]>();
            
            // Values with similar patterns
            for (int i = 0; i < 100; i++)
            {
                collidingValues.Add(new byte[] { (byte)i, 0, 0, 0 });
                collidingValues.Add(new byte[] { 0, (byte)i, 0, 0 });
                collidingValues.Add(new byte[] { 0, 0, (byte)i, 0 });
                collidingValues.Add(new byte[] { 0, 0, 0, (byte)i });
            }

            // Act
            var filter = XorFilter32.BuildFrom(collidingValues.ToArray());

            // Assert
            filter.Should().NotBeNull();
            
            // Verify all values are correctly stored
            foreach (var value in collidingValues)
            {
                filter.IsMember(value).Should().BeTrue();
            }
        }
    }

    /// <summary>
    /// Tests for specific scenarios that might cause infinite loops or hangs
    /// </summary>
    public class AntiHangTests
    {
        [Fact(Timeout = 30000)] // 30 second timeout
        public void BuildFrom_RepeatedHashPatterns_DoesNotHang()
        {
            // Arrange - Create values that produce similar hash patterns
            var values = new List<byte[]>();
            
            for (int i = 0; i < 1000; i++)
            {
                // Create patterns that might cause issues in the peeling process
                values.Add(BitConverter.GetBytes(i));
                values.Add(BitConverter.GetBytes(i + 1000000));
                values.Add(BitConverter.GetBytes(i + 2000000));
            }

            // Act
            var filter = XorFilter32.BuildFrom(values.ToArray());

            // Assert
            filter.Should().NotBeNull();
        }

        [Fact(Timeout = 30000)] // 30 second timeout
        public void BuildFrom_PowerOfTwoSizes_DoesNotHang()
        {
            // Arrange - Power of 2 sizes might cause issues in hash function distribution
            var sizes = new[] { 256, 512, 1024, 2048 };

            foreach (var size in sizes)
            {
                var values = Enumerable.Range(0, size)
                    .Select(i => BitConverter.GetBytes(i))
                    .ToArray();

                // Act
                var filter = XorFilter32.BuildFrom(values);

                // Assert
                filter.Should().NotBeNull();
                
                // Quick verification
                filter.IsMember(BitConverter.GetBytes(0)).Should().BeTrue();
                filter.IsMember(BitConverter.GetBytes(size - 1)).Should().BeTrue();
            }
        }

        [Fact(Timeout = 30000)] // 30 second timeout
        public void BuildFrom_WorstCaseScenario_DoesNotHang()
        {
            // Arrange - Create a scenario that's likely to require many peeling attempts
            var values = new List<byte[]>();
            
            // Create highly structured data that might cause peeling difficulties
            for (int i = 0; i < 500; i++)
            {
                values.Add(new byte[] { (byte)(i % 256), (byte)((i / 256) % 256), 0, 0 });
                values.Add(new byte[] { 0, 0, (byte)(i % 256), (byte)((i / 256) % 256) });
            }

            // Act
            var filter = XorFilter32.BuildFrom(values.ToArray());

            // Assert
            filter.Should().NotBeNull();
        }
    }
}
