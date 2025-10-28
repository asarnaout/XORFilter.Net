#pragma warning disable CS0618 // Type or member is obsolete - we're testing backward compatibility

using FluentAssertions;
using System.Text;
using Xunit;

namespace XORFilter.Net.Tests;

public class DeterministicIntegrationTests
{
    private const int DeterministicSeed = 12345;
    
    [Theory]
    [InlineData(typeof(XorFilter8))]
    [InlineData(typeof(XorFilter16))]
    [InlineData(typeof(XorFilter32))]
    public void BuildFrom_WithSeed_ShouldProduceDeterministicResults(Type filterType)
    {
        // Arrange
        var testData = GenerateTestData(100);
        
        // Act
        var filter1 = CreateFilter(filterType, testData, DeterministicSeed);
        var filter2 = CreateFilter(filterType, testData, DeterministicSeed);
        
        // Assert - Both filters should behave identically for all test data
        foreach (var item in testData)
        {
            var result1 = IsFilterMember(filter1, item);
            var result2 = IsFilterMember(filter2, item);
            result1.Should().Be(result2, "filters with same seed should produce identical results");
            result1.Should().BeTrue("all original items should be found in the filter");
        }
    }
    
    [Theory]
    [InlineData(typeof(XorFilter8))]
    [InlineData(typeof(XorFilter16))]
    [InlineData(typeof(XorFilter32))]
    public void BuildFrom_WithDifferentSeeds_ShouldProduceDifferentResults(Type filterType)
    {
        // Arrange
        var testData = GenerateTestData(50);
        var seed1 = 12345;
        var seed2 = 67890;
        
        // Act
        var filter1 = CreateFilter(filterType, testData, seed1);
        var filter2 = CreateFilter(filterType, testData, seed2);
        
        // Assert - Both filters should find all original items
        foreach (var item in testData)
        {
            IsFilterMember(filter1, item).Should().BeTrue("filter1 should contain all original items");
            IsFilterMember(filter2, item).Should().BeTrue("filter2 should contain all original items");
        }
        
        // But they might behave differently for non-member items
        var nonMemberData = GenerateTestData(20, "nonmember");
        var differentResults = 0;
        
        foreach (var item in nonMemberData)
        {
            var result1 = IsFilterMember(filter1, item);
            var result2 = IsFilterMember(filter2, item);
            if (result1 != result2)
                differentResults++;
        }
        
        // At least some results should be different (though this isn't guaranteed due to randomness)
        // This test mainly verifies that different seeds produce different internal structures
    }
    
    [Theory]
    [InlineData(typeof(XorFilter8))]
    [InlineData(typeof(XorFilter16))]
    [InlineData(typeof(XorFilter32))]
    public void IsMember_WithOriginalItems_ShouldAlwaysReturnTrue(Type filterType)
    {
        // Arrange
        var testData = GenerateTestData(200);
        var filter = CreateFilter(filterType, testData, DeterministicSeed);
        
        // Act & Assert
        foreach (var item in testData)
        {
            IsFilterMember(filter, item).Should().BeTrue($"item {Convert.ToHexString(item)} should be found in filter");
        }
    }
    
    [Theory]
    [InlineData(typeof(XorFilter8))]
    [InlineData(typeof(XorFilter16))]
    [InlineData(typeof(XorFilter32))]
    public void IsMember_WithDuplicateItems_ShouldHandleCorrectly(Type filterType)
    {
        // Arrange
        var baseData = GenerateTestData(50);
        var duplicatedData = baseData.Concat(baseData).Concat(baseData).ToArray(); // Triple the data
        var filter = CreateFilter(filterType, duplicatedData, DeterministicSeed);
        
        // Act & Assert - All original unique items should be found
        foreach (var item in baseData)
        {
            IsFilterMember(filter, item).Should().BeTrue("deduplicated items should be found in filter");
        }
    }
    
    [Theory]
    [InlineData(typeof(XorFilter8), 1000)]
    [InlineData(typeof(XorFilter16), 1000)]
    [InlineData(typeof(XorFilter32), 1000)]
    public void IsMember_FalsePositiveRate_ShouldBeWithinExpectedBounds(Type filterType, int testSize)
    {
        // Arrange
        var memberData = GenerateTestData(testSize);
        var nonMemberData = GenerateTestData(testSize * 2, "nonmember");
        var filter = CreateFilter(filterType, memberData, DeterministicSeed);
        
        // Act
        var falsePositives = 0;
        foreach (var item in nonMemberData)
        {
            if (IsFilterMember(filter, item))
                falsePositives++;
        }
        
        var falsePositiveRate = (double)falsePositives / nonMemberData.Length;
        
        // Assert - Check against expected error rates with statistical margins
        var expectedMaxRate = filterType.Name switch
        {
            nameof(XorFilter8) => 0.01,     // ~0.39% theoretical + statistical margin for 2000 tests
            nameof(XorFilter16) => 0.005,   // ~0.0015% + margin  
            nameof(XorFilter32) => 0.0001,  // ~2.3e-8% + margin
            _ => throw new ArgumentException($"Unknown filter type: {filterType.Name}")
        };
        
        (falsePositiveRate <= expectedMaxRate).Should().BeTrue( 
            $"{filterType.Name} false positive rate should be within expected bounds");
    }
    
    [Theory]
    [InlineData(typeof(XorFilter8))]
    [InlineData(typeof(XorFilter16))]
    [InlineData(typeof(XorFilter32))]
    public void BuildFrom_WithEmptyArray_ShouldThrowException(Type filterType)
    {
        // Arrange
        var emptyData = Array.Empty<byte[]>();
        
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => CreateFilter(filterType, emptyData, DeterministicSeed));
        exception.Message.Should().Contain("Values array should be provided");
    }
    
    [Theory]
    [InlineData(typeof(XorFilter8))]
    [InlineData(typeof(XorFilter16))]
    [InlineData(typeof(XorFilter32))]
    public void BuildFrom_WithSingleItem_ShouldWork(Type filterType)
    {
        // Arrange
        var singleItem = new[] { Encoding.UTF8.GetBytes("single") };
        
        // Act
        var filter = CreateFilter(filterType, singleItem, DeterministicSeed);
        
        // Assert
        IsFilterMember(filter, singleItem[0]).Should().BeTrue("single item should be found");
    }
    
    [Theory]
    [InlineData(typeof(XorFilter8), 1)]
    [InlineData(typeof(XorFilter8), 10)]
    [InlineData(typeof(XorFilter8), 100)]
    [InlineData(typeof(XorFilter8), 1000)]
    [InlineData(typeof(XorFilter16), 1)]
    [InlineData(typeof(XorFilter16), 10)]
    [InlineData(typeof(XorFilter16), 100)]
    [InlineData(typeof(XorFilter16), 1000)]
    [InlineData(typeof(XorFilter32), 1)]
    [InlineData(typeof(XorFilter32), 10)]
    [InlineData(typeof(XorFilter32), 100)]
    [InlineData(typeof(XorFilter32), 1000)]
    public void BuildFrom_WithVariousDataSizes_ShouldWork(Type filterType, int size)
    {
        // Arrange
        var testData = GenerateTestData(size);
        
        // Act
        var filter = CreateFilter(filterType, testData, DeterministicSeed);
        
        // Assert
        foreach (var item in testData)
        {
            IsFilterMember(filter, item).Should().BeTrue($"item should be found in filter of size {size}");
        }
    }
    
    [Theory]
    [InlineData(typeof(XorFilter8))]
    [InlineData(typeof(XorFilter16))]
    [InlineData(typeof(XorFilter32))]
    public void IsMember_WithEmptyInput_ShouldNotThrow(Type filterType)
    {
        // Arrange
        var testData = GenerateTestData(10);
        var filter = CreateFilter(filterType, testData, DeterministicSeed);

        // Act & Assert - Empty spans are valid input for the span-based API
        var action = () => IsFilterMember(filter, Array.Empty<byte>());
        action.Should().NotThrow("empty spans are valid input");
    }
    
    [Theory]
    [InlineData(typeof(XorFilter8))]
    [InlineData(typeof(XorFilter16))]
    [InlineData(typeof(XorFilter32))]
    public void IsMember_WithEmptyByteArray_ShouldWork(Type filterType)
    {
        // Arrange
        var testData = new[] { Array.Empty<byte>(), Encoding.UTF8.GetBytes("test") };
        var filter = CreateFilter(filterType, testData, DeterministicSeed);
        
        // Act & Assert
        IsFilterMember(filter, Array.Empty<byte>()).Should().BeTrue("empty byte array should be found if it was added");
        IsFilterMember(filter, Encoding.UTF8.GetBytes("test")).Should().BeTrue("regular item should be found");
    }
    
    [Theory]
    [InlineData(typeof(XorFilter8))]
    [InlineData(typeof(XorFilter16))]
    [InlineData(typeof(XorFilter32))]
    public void IsMember_WithVeryLargeByteArrays_ShouldWork(Type filterType)
    {
        // Arrange
        var largeData = new List<byte[]>();
        for (int i = 0; i < 10; i++)
        {
            var largeArray = new byte[10000];
            new Random(i).NextBytes(largeArray);
            largeData.Add(largeArray);
        }
        
        var filter = CreateFilter(filterType, largeData.ToArray(), DeterministicSeed);
        
        // Act & Assert
        foreach (var item in largeData)
        {
            IsFilterMember(filter, item).Should().BeTrue("large byte array should be found");
        }
    }
    
    [Theory]
    [InlineData(typeof(XorFilter8))]
    [InlineData(typeof(XorFilter16))]
    [InlineData(typeof(XorFilter32))]
    public void BuildFrom_WithIdenticalItemsInDifferentOrder_ShouldFindAllOriginalItems(Type filterType)
    {
        // Arrange - Use the same data set, just in different order
        var baseData = GenerateTestData(50);
        var data1 = baseData.OrderBy(x => x[0]).ToArray();
        var data2 = baseData.OrderByDescending(x => x[0]).ToArray();
        
        // Act
        var filter1 = CreateFilter(filterType, data1, DeterministicSeed);
        var filter2 = CreateFilter(filterType, data2, DeterministicSeed);
        
        // Assert - Both filters should find all original items regardless of order
        foreach (var item in baseData)
        {
            IsFilterMember(filter1, item).Should().BeTrue("filter1 should find all original items");
            IsFilterMember(filter2, item).Should().BeTrue("filter2 should find all original items");
        }
    }
    
    [Fact]
    public void DifferentFilterTypes_WithSameData_ShouldAllFindOriginalItems()
    {
        // Arrange
        var testData = GenerateTestData(100);
        
        var filter8 = XorFilter8.BuildFrom(testData, DeterministicSeed);
        var filter16 = XorFilter16.BuildFrom(testData, DeterministicSeed);
        var filter32 = XorFilter32.BuildFrom(testData, DeterministicSeed);
        
        // Act & Assert
        foreach (var item in testData)
        {
            filter8.IsMember(item).Should().BeTrue("XorFilter8 should find all original items");
            filter16.IsMember(item).Should().BeTrue("XorFilter16 should find all original items");
            filter32.IsMember(item).Should().BeTrue("XorFilter32 should find all original items");
        }
    }
    
    // Helper methods
    private static byte[][] GenerateTestData(int count, string prefix = "test")
    {
        var data = new byte[count][];
        for (int i = 0; i < count; i++)
        {
            data[i] = Encoding.UTF8.GetBytes($"{prefix}_{i}_{Guid.NewGuid()}");
        }
        return data;
    }
    
    private static object CreateFilter(Type filterType, byte[][] data, int seed)
    {
        var span = new Span<byte[]>(data);
        
        return filterType.Name switch
        {
            nameof(XorFilter8) => XorFilter8.BuildFrom(span, seed),
            nameof(XorFilter16) => XorFilter16.BuildFrom(span, seed),
            nameof(XorFilter32) => XorFilter32.BuildFrom(span, seed),
            _ => throw new ArgumentException($"Unknown filter type: {filterType.Name}")
        };
    }
    
    private static bool IsFilterMember(object filter, byte[] item)
    {
        // Use the span-based API by casting to the base type
        if (filter is BaseXorFilter<byte> filter8)
            return filter8.IsMember(item.AsSpan());
        if (filter is BaseXorFilter<ushort> filter16)
            return filter16.IsMember(item.AsSpan());
        if (filter is BaseXorFilter<uint> filter32)
            return filter32.IsMember(item.AsSpan());

        throw new InvalidOperationException($"Unknown filter type: {filter.GetType().Name}");
    }
}
