using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace XORFilter.Net.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class PerformanceBenchmarks
{
    private readonly List<byte[]> _testDataSet = new();
    private readonly List<byte[]> _lookupTestSet = new();
    
    private XorFilter8? _xorFilter8;
    private XorFilter16? _xorFilter16;
    private XorFilter32? _xorFilter32;
    private SimpleBloomFilter? _bloomFilter;

    [Params(10000, 100000)]
    public int DataSetSize { get; set; }

    [Params(0.01, 0.001)]
    public double BloomFalsePositiveRate { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        
        // Generate test dataset
        _testDataSet.Clear();
        for (var i = 0; i < DataSetSize; i++)
        {
            var data = new byte[16];
            random.NextBytes(data);
            _testDataSet.Add(data);
        }

        // Generate lookup test set (mix of members and non-members)
        _lookupTestSet.Clear();
        for (var i = 0; i < 1000; i++)
        {
            if (i % 2 == 0 && i / 2 < _testDataSet.Count)
            {
                // Add existing members
                _lookupTestSet.Add(_testDataSet[i / 2]);
            }
            else
            {
                // Add non-members
                var data = new byte[16];
                random.NextBytes(data);
                _lookupTestSet.Add(data);
            }
        }

        // Build filters
        _xorFilter8 = XorFilter8.BuildFrom(_testDataSet.ToArray(), 42);
        _xorFilter16 = XorFilter16.BuildFrom(_testDataSet.ToArray(), 42);
        _xorFilter32 = XorFilter32.BuildFrom(_testDataSet.ToArray(), 42);

    _bloomFilter = new SimpleBloomFilter(DataSetSize, BloomFalsePositiveRate);

        foreach (var item in _testDataSet)
        {
            _bloomFilter.Add(item);
        }
    }

    [Benchmark]
    public object XorFilter8_Construction()
    {
        return XorFilter8.BuildFrom(_testDataSet.ToArray(), 42);
    }

    [Benchmark]
    public object XorFilter16_Construction()
    {
        return XorFilter16.BuildFrom(_testDataSet.ToArray(), 42);
    }

    [Benchmark]
    public object XorFilter32_Construction()
    {
        return XorFilter32.BuildFrom(_testDataSet.ToArray(), 42);
    }

    [Benchmark]
    public object BloomFilter_Construction()
    {
        var filter = new SimpleBloomFilter(DataSetSize, BloomFalsePositiveRate);
        foreach (var item in _testDataSet)
        {
            filter.Add(item);
        }
        return filter;
    }

    [Benchmark]
    public int XorFilter8_LookupOperations()
    {
        var hits = 0;
        foreach (var item in _lookupTestSet)
        {
            if (_xorFilter8!.IsMember(item))
                hits++;
        }
        return hits;
    }

    [Benchmark]
    public int XorFilter16_LookupOperations()
    {
        var hits = 0;
        foreach (var item in _lookupTestSet)
        {
            if (_xorFilter16!.IsMember(item))
                hits++;
        }
        return hits;
    }

    [Benchmark]
    public int XorFilter32_LookupOperations()
    {
        var hits = 0;
        foreach (var item in _lookupTestSet)
        {
            if (_xorFilter32!.IsMember(item))
                hits++;
        }
        return hits;
    }

    [Benchmark]
    public int BloomFilter_LookupOperations()
    {
        var hits = 0;
        foreach (var item in _lookupTestSet)
        {
            if (_bloomFilter!.Contains(item))
                hits++;
        }
        return hits;
    }
}
