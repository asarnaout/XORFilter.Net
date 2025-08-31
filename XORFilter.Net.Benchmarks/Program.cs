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
        Console.WriteLine("Performance & Memory: XOR Filters | False Positives: XOR vs Bloom");
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
        sb.AppendLine();
        
        sb.AppendLine("## Key Advantages of XOR Filters:");
        sb.AppendLine("✓ **Faster Lookups**: XOR filters require exactly 3 memory accesses");
        sb.AppendLine("✓ **Better Cache Performance**: More predictable memory access patterns");
        sb.AppendLine("✓ **Space Efficient**: ~1.23x the number of keys for storage");
        sb.AppendLine("✓ **No False Negatives**: Guaranteed accurate for all inserted elements");
        sb.AppendLine("✓ **Deterministic**: Same input always produces same filter");
        sb.AppendLine();
        
        sb.AppendLine("## XOR Filter Comparison:");
        sb.AppendLine("- **XorFilter8**: Lowest memory usage, moderate false positive rate (~0.39%)");
        sb.AppendLine("- **XorFilter16**: Balanced memory and accuracy (~0.0015% false positive rate)");
        sb.AppendLine("- **XorFilter32**: Highest memory usage, extremely low false positive rate (~2.33e-8%)");
        sb.AppendLine();
        
        sb.AppendLine("## Use Case Recommendations:");
        sb.AppendLine("- **Use XorFilter8** when memory is constrained and moderate false positive rates are acceptable");
        sb.AppendLine("- **Use XorFilter16** for most general-purpose applications requiring good accuracy");
        sb.AppendLine("- **Use XorFilter32** when extremely high accuracy is required and memory is not a constraint");
        
        Console.WriteLine(sb.ToString());
    }
}
