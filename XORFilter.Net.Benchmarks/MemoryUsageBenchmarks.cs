using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace XORFilter.Net.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class MemoryUsageBenchmarks
{
    private readonly List<byte[]> _testDataSet = new();

    [Params(10000, 100000, 1000000)]
    public int DataSetSize { get; set; }

    [Params(0.01, 0.001, 0.0001)]
    public double BloomFalsePositiveRate { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        
        _testDataSet.Clear();
        for (var i = 0; i < DataSetSize; i++)
        {
            var data = new byte[16];
            random.NextBytes(data);
            _testDataSet.Add(data);
        }
    }

    [Benchmark]
    public MemoryInfo XorFilter8_MemoryUsage()
    {
        var memoryBefore = GC.GetTotalMemory(true);
        var filter = XorFilter8.BuildFrom(_testDataSet.ToArray(), 42);
        var memoryAfter = GC.GetTotalMemory(false);
        
        return new MemoryInfo
        {
            FilterType = "XorFilter8",
            DataSetSize = DataSetSize,
            EstimatedMemoryBytes = memoryAfter - memoryBefore,
            BitsPerElement = (double)(memoryAfter - memoryBefore) * 8 / DataSetSize,
            Filter = filter
        };
    }

    [Benchmark]
    public MemoryInfo XorFilter16_MemoryUsage()
    {
        var memoryBefore = GC.GetTotalMemory(true);
        var filter = XorFilter16.BuildFrom(_testDataSet.ToArray(), 42);
        var memoryAfter = GC.GetTotalMemory(false);
        
        return new MemoryInfo
        {
            FilterType = "XorFilter16",
            DataSetSize = DataSetSize,
            EstimatedMemoryBytes = memoryAfter - memoryBefore,
            BitsPerElement = (double)(memoryAfter - memoryBefore) * 8 / DataSetSize,
            Filter = filter
        };
    }

    [Benchmark]
    public MemoryInfo XorFilter32_MemoryUsage()
    {
        var memoryBefore = GC.GetTotalMemory(true);
        var filter = XorFilter32.BuildFrom(_testDataSet.ToArray(), 42);
        var memoryAfter = GC.GetTotalMemory(false);
        
        return new MemoryInfo
        {
            FilterType = "XorFilter32",
            DataSetSize = DataSetSize,
            EstimatedMemoryBytes = memoryAfter - memoryBefore,
            BitsPerElement = (double)(memoryAfter - memoryBefore) * 8 / DataSetSize,
            Filter = filter
        };
    }

    [Benchmark]
    public MemoryInfo BloomFilter_MemoryUsage()
    {
        var memoryBefore = GC.GetTotalMemory(true);
    var filter = new SimpleBloomFilter(DataSetSize, BloomFalsePositiveRate);
        
        foreach (var item in _testDataSet)
        {
            filter.Add(item);
        }
        
        var memoryAfter = GC.GetTotalMemory(false);
        
        return new MemoryInfo
        {
            FilterType = "BloomFilter",
            DataSetSize = DataSetSize,
            EstimatedMemoryBytes = memoryAfter - memoryBefore,
            BitsPerElement = (double)(memoryAfter - memoryBefore) * 8 / DataSetSize,
            Filter = filter
        };
    }
}

public class MemoryInfo
{
    public string FilterType { get; set; } = string.Empty;
    public int DataSetSize { get; set; }
    public long EstimatedMemoryBytes { get; set; }
    public double BitsPerElement { get; set; }
    public object? Filter { get; set; }

    public override string ToString()
    {
        return $"{FilterType}: {EstimatedMemoryBytes:N0} bytes ({BitsPerElement:F2} bits/element) - Dataset: {DataSetSize:N0}";
    }
}
