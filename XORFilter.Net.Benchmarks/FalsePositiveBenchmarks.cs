using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System.IO.Hashing;
using System.Buffers.Binary;
using System.Collections;

namespace XORFilter.Net.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class FalsePositiveBenchmarks
{
    private readonly List<byte[]> _testDataSet = new();
    private readonly List<byte[]> _nonMemberTestSet = new();
    
    private XorFilter8? _xorFilter8;
    private XorFilter16? _xorFilter16;
    private XorFilter32? _xorFilter32;
    private SimpleBloomFilter? _bloomFilter;

    [Params(1000, 10000, 100000)]
    public int DataSetSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42); // Fixed seed for reproducible results
        
        // Generate test dataset
        _testDataSet.Clear();
        for (var i = 0; i < DataSetSize; i++)
        {
            var data = new byte[16]; // 16-byte keys
            random.NextBytes(data);
            _testDataSet.Add(data);
        }

        // Generate non-member test set (guaranteed not in the original set)
        _nonMemberTestSet.Clear();
        var usedHashes = new HashSet<string>(_testDataSet.Select(x => Convert.ToBase64String(x)));
        
        for (var i = 0; i < DataSetSize; i++)
        {
            byte[] data;
            string hash;
            do
            {
                data = new byte[16];
                random.NextBytes(data);
                hash = Convert.ToBase64String(data);
            } while (usedHashes.Contains(hash));
            
            _nonMemberTestSet.Add(data);
            usedHashes.Add(hash);
        }

        // Build XOR filters
        _xorFilter8 = XorFilter8.BuildFrom(_testDataSet.ToArray(), 42);
        _xorFilter16 = XorFilter16.BuildFrom(_testDataSet.ToArray(), 42);
        _xorFilter32 = XorFilter32.BuildFrom(_testDataSet.ToArray(), 42);

        // Build Bloom filter with comparable false positive rate to XorFilter16
    var expectedFalsePositiveRate = 0.000015; // ~0.0015% similar to XorFilter16
    _bloomFilter = new SimpleBloomFilter(DataSetSize, expectedFalsePositiveRate);

        foreach (var item in _testDataSet)
        {
            _bloomFilter.Add(item);
        }
    }

    [Benchmark]
    public FalsePositiveResult XorFilter8_FalsePositiveTest()
    {
        return TestFalsePositives(_xorFilter8!.IsMember, "XorFilter8");
    }

    [Benchmark]
    public FalsePositiveResult XorFilter16_FalsePositiveTest()
    {
        return TestFalsePositives(_xorFilter16!.IsMember, "XorFilter16");
    }

    [Benchmark]
    public FalsePositiveResult XorFilter32_FalsePositiveTest()
    {
        return TestFalsePositives(_xorFilter32!.IsMember, "XorFilter32");
    }

    [Benchmark]
    public FalsePositiveResult BloomFilter_FalsePositiveTest()
    {
        return TestFalsePositives(_bloomFilter!.Contains, "BloomFilter");
    }

    private FalsePositiveResult TestFalsePositives(Func<byte[], bool> isMemberFunc, string filterType)
    {
        var falsePositives = 0;
        var totalTests = _nonMemberTestSet.Count;

        foreach (var item in _nonMemberTestSet)
        {
            if (isMemberFunc(item))
            {
                falsePositives++;
            }
        }

        var falsePositiveRate = (double)falsePositives / totalTests;
        
        return new FalsePositiveResult
        {
            FilterType = filterType,
            FalsePositives = falsePositives,
            TotalTests = totalTests,
            FalsePositiveRate = falsePositiveRate,
            DataSetSize = DataSetSize
        };
    }
}

public class FalsePositiveResult
{
    public string FilterType { get; set; } = string.Empty;
    public int FalsePositives { get; set; }
    public int TotalTests { get; set; }
    public double FalsePositiveRate { get; set; }
    public int DataSetSize { get; set; }

    public override string ToString()
    {
        return $"{FilterType}: {FalsePositives}/{TotalTests} ({FalsePositiveRate:P4}) - Dataset: {DataSetSize}";
    }
}

public class SimpleBloomFilter
{
    private readonly BitArray _bitArray;
    private readonly int _hashFunctionCount;
    private readonly int _size;
    private readonly ulong _seed1;
    private readonly ulong _seed2;

    public SimpleBloomFilter(int expectedElements, double falsePositiveRate, ulong seed1 = 0xCBF29CE484222325UL, ulong seed2 = 0x9E3779B97F4A7C15UL)
    {
        if (expectedElements <= 0) throw new ArgumentOutOfRangeException(nameof(expectedElements));
        if (falsePositiveRate <= 0 || falsePositiveRate >= 1) throw new ArgumentOutOfRangeException(nameof(falsePositiveRate));

        // Calculate optimal bit array size and hash function count
        _size = (int)Math.Ceiling(-expectedElements * Math.Log(falsePositiveRate) / (Math.Log(2) * Math.Log(2)));
        _hashFunctionCount = (int)Math.Ceiling((_size / (double)expectedElements) * Math.Log(2));
        _bitArray = new BitArray(_size);
        _seed1 = seed1;
        _seed2 = seed2;
    }

    public void Add(byte[] item)
    {
        foreach (var index in GetIndices(item))
        {
            _bitArray[index] = true;
        }
    }

    public bool Contains(byte[] item)
    {
        foreach (var index in GetIndices(item))
        {
            if (!_bitArray[index])
                return false;
        }
        return true;
    }

    private static ulong Hash64WithSeed(ReadOnlySpan<byte> item, ulong seed)
    {
        Span<byte> buf = stackalloc byte[8 + item.Length];
        BinaryPrimitives.WriteUInt64LittleEndian(buf, seed);
        item.CopyTo(buf.Slice(8));
        var hashBytes = XxHash64.Hash(buf);
        return BinaryPrimitives.ReadUInt64LittleEndian(hashBytes);
    }

    private IEnumerable<int> GetIndices(byte[] item)
    {
        // Double hashing (Kirsch-Mitzenmacher): index_i = (h1 + i*h2) mod m
        // Use fast non-cryptographic hash (XxHash64) with two different seeds.
        var h1 = Hash64WithSeed(item, _seed1);
        var h2 = Hash64WithSeed(item, _seed2);

        // Ensure h2 is odd to better cover the space if it happens to be 0
        h2 = (h2 << 1) | 1UL;

        var m = (ulong)_size;
        for (int i = 0; i < _hashFunctionCount; i++)
        {
            var combined = h1 + (ulong)i * h2;
            var idx = (int)(combined % m);
            yield return idx;
        }
    }
}
