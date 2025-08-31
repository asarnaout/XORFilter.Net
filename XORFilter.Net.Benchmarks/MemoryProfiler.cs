using System.Diagnostics;
using System.Runtime;

namespace XORFilter.Net.Benchmarks;

/// <summary>
/// Utility class for more accurate memory profiling in benchmarks
/// </summary>
public static class MemoryProfiler
{
    /// <summary>
    /// Performs aggressive garbage collection to ensure clean memory state
    /// </summary>
    public static void ForceCleanMemoryState()
    {
        // Multiple rounds of collection to ensure everything is cleaned
        for (int i = 0; i < 3; i++)
        {
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
        }
        
        // Small delay to let memory settle
        Thread.Sleep(10);
    }

    /// <summary>
    /// Gets stable memory reading by taking multiple samples
    /// </summary>
    public static long GetStableMemoryReading(int samples = 5)
    {
        var readings = new long[samples];
        for (int i = 0; i < samples; i++)
        {
            readings[i] = GC.GetTotalMemory(false);
            if (i < samples - 1) 
                Thread.Sleep(1); // Small delay between readings
        }
        
        // Return median of readings for stability
        Array.Sort(readings);
        return readings[samples / 2];
    }

    /// <summary>
    /// Measures memory allocation of an operation with statistical analysis
    /// </summary>
    public static MemoryMeasurement MeasureAllocation<T>(Func<T> operation, int samples = 15)
    {
        var measurements = new List<long>();
        
        // Warmup to stabilize JIT and memory state
        for (int i = 0; i < 5; i++)
        {
            var warmup = operation();
            GC.KeepAlive(warmup);
            ForceCleanMemoryState();
        }

        // Take measurements
        for (int sample = 0; sample < samples; sample++)
        {
            ForceCleanMemoryState();
            
            var memoryBefore = GetStableMemoryReading();
            var result = operation();
            var memoryAfter = GetStableMemoryReading();
            
            var allocated = memoryAfter - memoryBefore;
            if (allocated >= 0) // Only count positive allocations
            {
                measurements.Add(allocated);
            }
            
            GC.KeepAlive(result);
        }

        return AnalyzeMeasurements(measurements);
    }

    private static MemoryMeasurement AnalyzeMeasurements(List<long> measurements)
    {
        if (measurements.Count == 0)
        {
            return new MemoryMeasurement
            {
                Median = 0,
                Average = 0,
                Minimum = 0,
                Maximum = 0,
                StandardDeviation = 0,
                SampleCount = 0
            };
        }

        // Remove outliers using IQR method
        var cleaned = RemoveOutliers(measurements);
        
        var sorted = cleaned.OrderBy(x => x).ToList();
        var median = sorted[sorted.Count / 2];
        var average = cleaned.Average();
        var min = cleaned.Min();
        var max = cleaned.Max();
        var stdDev = cleaned.Count > 1 ? 
            Math.Sqrt(cleaned.Select(x => Math.Pow(x - average, 2)).Sum() / (cleaned.Count - 1)) : 0;

        return new MemoryMeasurement
        {
            Median = median,
            Average = (long)average,
            Minimum = min,
            Maximum = max,
            StandardDeviation = stdDev,
            SampleCount = cleaned.Count,
            OriginalSampleCount = measurements.Count
        };
    }

    private static List<long> RemoveOutliers(List<long> data)
    {
        if (data.Count <= 4) return data; // Need enough data for quartile calculation

        var sorted = data.OrderBy(x => x).ToList();
        var q1Index = sorted.Count / 4;
        var q3Index = 3 * sorted.Count / 4;
        var q1 = sorted[q1Index];
        var q3 = sorted[q3Index];
        var iqr = q3 - q1;
        var lowerBound = q1 - 1.5 * iqr;
        var upperBound = q3 + 1.5 * iqr;

        return data.Where(x => x >= lowerBound && x <= upperBound).ToList();
    }
}

/// <summary>
/// Results of memory measurement analysis
/// </summary>
public class MemoryMeasurement
{
    public long Median { get; set; }
    public long Average { get; set; }
    public long Minimum { get; set; }
    public long Maximum { get; set; }
    public double StandardDeviation { get; set; }
    public int SampleCount { get; set; }
    public int OriginalSampleCount { get; set; }

    public override string ToString()
    {
        return $"Median: {Median:N0} bytes, Avg: {Average:N0} ± {StandardDeviation:F0}, " +
               $"Range: [{Minimum:N0}-{Maximum:N0}], Samples: {SampleCount}/{OriginalSampleCount}";
    }
}
