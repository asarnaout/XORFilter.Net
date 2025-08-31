using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System.Runtime;

namespace XORFilter.Net.Benchmarks;

/// <summary>
/// Enhanced Memory Usage Benchmarks with Statistical Analysis
/// 
/// Improvements over basic GC.GetTotalMemory() approach:
/// 1. Multiple samples with outlier removal using IQR method
/// 2. Aggressive GC stabilization before measurements
/// 3. Multiple memory readings averaged for stability
/// 4. Baseline measurements to understand measurement noise
/// 5. Theoretical calculations for validation
/// 6. Statistical reporting (mean, median, std dev, range)
/// 
/// The measurements use median values as they are more robust against outliers
/// than mean values in memory allocation scenarios.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class MemoryUsageBenchmarks
{
    private readonly List<byte[]> _testDataSet = new();

    [Params(10000, 100000, 1000000)]
    public int DataSetSize { get; set; }

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

    /// <summary>
    /// Baseline measurement to understand measurement overhead and noise
    /// </summary>
    [Benchmark]
    public MemoryInfo Baseline_MeasurementOverhead()
    {
        return MeasureFilterMemoryUsageEnhanced(() => new object(), "Baseline_Empty");
    }

    /// <summary>
    /// Known memory allocation for validation
    /// </summary>
    [Benchmark] 
    public MemoryInfo Baseline_KnownAllocation()
    {
        return MeasureFilterMemoryUsageEnhanced(() => new byte[DataSetSize], "Baseline_KnownBytes");
    }

    /// <summary>
    /// Enhanced memory measurement using the MemoryProfiler utility
    /// </summary>
    private MemoryInfo MeasureFilterMemoryUsageEnhanced<T>(Func<T> filterFactory, string filterType) where T : class
    {
        var measurement = MemoryProfiler.MeasureAllocation(filterFactory);

        return new MemoryInfo
        {
            FilterType = filterType,
            DataSetSize = DataSetSize,
            EstimatedMemoryBytes = measurement.Median,
            BitsPerElement = (double)measurement.Median * 8 / DataSetSize,
            MinMemoryBytes = measurement.Minimum,
            MaxMemoryBytes = measurement.Maximum,
            AverageMemoryBytes = measurement.Average,
            StandardDeviation = measurement.StandardDeviation,
            SampleCount = measurement.SampleCount
        };
    }

    [Benchmark]
    public MemoryInfo XorFilter8_MemoryUsage()
    {
        return MeasureFilterMemoryUsageEnhanced(
            () => XorFilter8.BuildFrom(_testDataSet.ToArray(), 42),
            "XorFilter8"
        );
    }

    [Benchmark]
    public MemoryInfo XorFilter16_MemoryUsage()
    {
        return MeasureFilterMemoryUsageEnhanced(
            () => XorFilter16.BuildFrom(_testDataSet.ToArray(), 42),
            "XorFilter16"
        );
    }

    [Benchmark]
    public MemoryInfo XorFilter32_MemoryUsage()
    {
        return MeasureFilterMemoryUsageEnhanced(
            () => XorFilter32.BuildFrom(_testDataSet.ToArray(), 42),
            "XorFilter32"
        );
    }

    /// <summary>
    /// Provides theoretical memory usage for validation against measured values
    /// </summary>
    [Benchmark]
    public MemoryInfo XorFilter8_TheoreticalMemory()
    {
        // XorFilter8 uses: tableSize * sizeof(byte) + overhead
        // tableSize = Math.Ceiling(DataSetSize * 1.23) 
        var theoreticalTableSize = (int)Math.Ceiling(DataSetSize * 1.23d);
        var theoreticalMemory = theoreticalTableSize * sizeof(byte);
        
        return new MemoryInfo
        {
            FilterType = "XorFilter8_Theoretical",
            DataSetSize = DataSetSize,
            EstimatedMemoryBytes = theoreticalMemory,
            BitsPerElement = (double)theoreticalMemory * 8 / DataSetSize,
            MinMemoryBytes = theoreticalMemory,
            MaxMemoryBytes = theoreticalMemory,
            AverageMemoryBytes = theoreticalMemory,
            StandardDeviation = 0,
            SampleCount = 1
        };
    }

    [Benchmark]
    public MemoryInfo XorFilter16_TheoreticalMemory()
    {
        var theoreticalTableSize = (int)Math.Ceiling(DataSetSize * 1.23d);
        var theoreticalMemory = theoreticalTableSize * sizeof(ushort);
        
        return new MemoryInfo
        {
            FilterType = "XorFilter16_Theoretical",
            DataSetSize = DataSetSize,
            EstimatedMemoryBytes = theoreticalMemory,
            BitsPerElement = (double)theoreticalMemory * 8 / DataSetSize,
            MinMemoryBytes = theoreticalMemory,
            MaxMemoryBytes = theoreticalMemory,
            AverageMemoryBytes = theoreticalMemory,
            StandardDeviation = 0,
            SampleCount = 1
        };
    }

    [Benchmark]
    public MemoryInfo XorFilter32_TheoreticalMemory()
    {
        var theoreticalTableSize = (int)Math.Ceiling(DataSetSize * 1.23d);
        var theoreticalMemory = theoreticalTableSize * sizeof(uint);
        
        return new MemoryInfo
        {
            FilterType = "XorFilter32_Theoretical",
            DataSetSize = DataSetSize,
            EstimatedMemoryBytes = theoreticalMemory,
            BitsPerElement = (double)theoreticalMemory * 8 / DataSetSize,
            MinMemoryBytes = theoreticalMemory,
            MaxMemoryBytes = theoreticalMemory,
            AverageMemoryBytes = theoreticalMemory,
            StandardDeviation = 0,
            SampleCount = 1
        };
    }
}

public class MemoryInfo
{
    public string FilterType { get; set; } = string.Empty;
    public int DataSetSize { get; set; }
    public long EstimatedMemoryBytes { get; set; }
    public double BitsPerElement { get; set; }
    
    // Statistical measurements for accuracy
    public long MinMemoryBytes { get; set; }
    public long MaxMemoryBytes { get; set; }
    public long AverageMemoryBytes { get; set; }
    public double StandardDeviation { get; set; }
    public int SampleCount { get; set; }

    public override string ToString()
    {
        return $"{FilterType}: {EstimatedMemoryBytes:N0} bytes ({BitsPerElement:F2} bits/element) " +
               $"± {StandardDeviation:F0} bytes [Range: {MinMemoryBytes:N0}-{MaxMemoryBytes:N0}] " +
               $"(n={SampleCount}) - Dataset: {DataSetSize:N0}";
    }
}
