# XORFilter.Net Comprehensive Benchmarks

This project provides comprehensive benchmarks comparing XORFilter.Net against traditional Bloom Filters, focusing on:

## 🎯 Benchmark Categories

### 1. False Positive Rate Analysis
- **Purpose**: Compare the accuracy of XOR filters vs Bloom filters
- **Metrics**: False positive rates across different data set sizes
- **Key Insights**: Shows theoretical vs actual false positive rates

### 2. Performance Benchmarks
- **Construction Time**: How fast filters can be built
- **Lookup Performance**: Speed of membership queries
- **Throughput**: Operations per second

### 3. Memory Usage Analysis
- **Memory Efficiency**: Bytes per element
- **Space Overhead**: Comparison of storage requirements
- **Scalability**: How memory usage scales with data size

## 🚀 Running Benchmarks

### Run All Benchmarks
```bash
dotnet run --configuration Release
```

### Run Specific Benchmark Categories
```bash
# False Positive Rate Analysis
dotnet run --configuration Release -- fp

# Performance Analysis  
dotnet run --configuration Release -- perf

# Memory Usage Analysis
dotnet run --configuration Release -- mem
```

## 📊 Expected Results

### False Positive Rates (Theoretical)
| Filter Type  | Theoretical FP Rate | Bits per Element |
|--------------|-------------------|------------------|
| XorFilter8   | ~0.390625%        | ~9.84 bits       |
| XorFilter16  | ~0.0015%          | ~19.69 bits      |
| XorFilter32  | ~2.33e-8%         | ~39.38 bits      |
| BloomFilter  | Configurable      | Variable         |

### Performance Characteristics
- **XOR Filters**: Exactly 3 memory accesses per lookup
- **Bloom Filters**: Variable memory accesses (typically 3-7)
- **Construction**: XOR filters require initial peeling algorithm
- **Lookups**: XOR filters generally faster due to predictable access patterns

### Memory Usage
- **XOR Filters**: ~1.23x the number of elements
- **Bloom Filters**: Depends on desired false positive rate
- **Cache Performance**: XOR filters have better spatial locality

## 🔍 Key Comparisons

### When to Use XOR Filters
✅ **Static datasets** - No need to add elements after construction  
✅ **Fast lookups** - Predictable memory access patterns  
✅ **Space efficiency** - Lower memory overhead  
✅ **Cache performance** - Better spatial locality  
✅ **Deterministic** - Same input always produces same filter  

### When to Use Bloom Filters
✅ **Dynamic datasets** - Can add elements after construction  
✅ **Flexible FP rates** - Configurable false positive rates  
✅ **Incremental building** - Can build filters incrementally  
✅ **Mature ecosystem** - Well-established implementations  

## 📈 Benchmark Configuration

- **Framework**: BenchmarkDotNet
- **Runtime**: .NET 8.0
- **Strategy**: Throughput
- **Iterations**: 5 iterations with 3 warmup rounds
- **Memory Profiling**: Enabled
- **Export Formats**: CSV, Markdown

## 🧪 Test Data

- **Key Size**: 16 bytes (128-bit keys)
- **Data Sets**: 1K, 10K, 100K, 1M elements
- **Random Seed**: Fixed at 42 for reproducible results
- **Test Mix**: 50% members, 50% non-members for lookup tests

## 📋 Output Files

Benchmark results are exported to:
- `BenchmarkDotNet.Artifacts/` - Detailed results
- CSV files for data analysis
- Markdown summaries for documentation

## 🔧 Requirements

- .NET 8.0 SDK
- BenchmarkDotNet package
- BloomFilter.NetCore package
- XORFilter.Net library (project reference)

## 🎓 Understanding Results

### Performance Metrics
- **Mean**: Average execution time
- **StdDev**: Standard deviation (consistency)
- **Allocated**: Memory allocated per operation
- **Gen0/Gen1/Gen2**: Garbage collection statistics

### Memory Metrics
- **Allocated**: Total memory allocated
- **Gen0-2 Collections**: GC pressure indicators
- **Bytes per Element**: Space efficiency measure

## 📚 Further Reading

- [XOR Filters: Faster and Smaller Than Bloom Filters](https://arxiv.org/abs/1912.08258)
- [Bloom Filter Wikipedia](https://en.wikipedia.org/wiki/Bloom_filter)
- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
