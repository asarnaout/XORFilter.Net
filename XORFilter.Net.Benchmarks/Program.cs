using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using System.Text;
using XORFilter.Net.Benchmarks;

namespace XORFilter.Net.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("==========================================================");
        Console.WriteLine("XORFilter.Net Comprehensive Benchmarks");
        Console.WriteLine("Comparing XOR Filters vs Bloom Filters");
        Console.WriteLine("==========================================================");
        Console.WriteLine();

        var config = DefaultConfig.Instance
            .AddExporter(CsvMeasurementsExporter.Default)
            .AddExporter(MarkdownExporter.GitHub)
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddColumn(StatisticColumn.Mean)
            .AddColumn(StatisticColumn.StdDev)
            .AddColumn(StatisticColumn.Min)
            .AddColumn(StatisticColumn.Max)
            .AddJob(Job.Default
                .WithRuntime(CoreRuntime.Core80)
                .WithStrategy(RunStrategy.Throughput)
                .WithLaunchCount(1)
                .WithWarmupCount(3)
                .WithIterationCount(5));

        if (args.Length > 0)
        {
            switch (args[0].ToLower())
            {
                case "false-positive":
                case "fp":
                    Console.WriteLine("Running False Positive Rate Benchmarks...");
                    BenchmarkRunner.Run<FalsePositiveBenchmarks>(config);
                    break;

                case "performance":
                case "perf":
                    Console.WriteLine("Running Performance Benchmarks...");
                    BenchmarkRunner.Run<PerformanceBenchmarks>(config);
                    break;

                case "memory":
                case "mem":
                    Console.WriteLine("Running Memory Usage Benchmarks...");
                    BenchmarkRunner.Run<MemoryUsageBenchmarks>(config);
                    break;

                case "all":
                default:
                    RunAllBenchmarks(config);
                    break;
            }
        }
        else
        {
            RunAllBenchmarks(config);
        }

        Console.WriteLine();
        Console.WriteLine("==========================================================");
        Console.WriteLine("Benchmark Summary:");
        Console.WriteLine("==========================================================");
        PrintTheoricalComparison();
    }

    private static void RunAllBenchmarks(IConfig config)
    {
        Console.WriteLine("Running ALL Benchmarks (this may take a while)...");
        Console.WriteLine();

        Console.WriteLine("1. False Positive Rate Analysis...");
        BenchmarkRunner.Run<FalsePositiveBenchmarks>(config);
        
        Console.WriteLine("\n2. Performance Analysis...");
        BenchmarkRunner.Run<PerformanceBenchmarks>(config);
        
        Console.WriteLine("\n3. Memory Usage Analysis...");
        BenchmarkRunner.Run<MemoryUsageBenchmarks>(config);
    }

    private static void PrintTheoricalComparison()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("## Theoretical False Positive Rates:");
        sb.AppendLine("| Filter Type  | Theoretical FP Rate | Bits per Element |");
        sb.AppendLine("|--------------|-------------------|------------------|");
        sb.AppendLine("| XorFilter8   | ~0.390625%        | ~9.84 bits       |");
        sb.AppendLine("| XorFilter16  | ~0.0015%          | ~19.69 bits      |");
        sb.AppendLine("| XorFilter32  | ~2.33e-8%         | ~39.38 bits      |");
        sb.AppendLine("| BloomFilter  | Configurable      | Variable         |");
        sb.AppendLine();
        
        sb.AppendLine("## Key Advantages of XOR Filters:");
        sb.AppendLine("✓ **Faster Lookups**: XOR filters require exactly 3 memory accesses");
        sb.AppendLine("✓ **Better Cache Performance**: More predictable memory access patterns");
        sb.AppendLine("✓ **Space Efficient**: ~1.23x the number of keys for storage");
        sb.AppendLine("✓ **No False Negatives**: Guaranteed accurate for all inserted elements");
        sb.AppendLine("✓ **Deterministic**: Same input always produces same filter");
        sb.AppendLine();
        
        sb.AppendLine("## Bloom Filter Advantages:");
        sb.AppendLine("✓ **Dynamic Insertion**: Can add elements after construction");
        sb.AppendLine("✓ **Tunable False Positive Rate**: Can be configured at construction");
        sb.AppendLine("✓ **Mature Ecosystem**: Well-established with many implementations");
        sb.AppendLine();
        
        sb.AppendLine("## Use Case Recommendations:");
        sb.AppendLine("- **Use XOR Filters** when you have a static dataset and need fast, cache-friendly lookups");
        sb.AppendLine("- **Use Bloom Filters** when you need to add elements dynamically or require very specific FP rates");
        
        Console.WriteLine(sb.ToString());
    }
}
