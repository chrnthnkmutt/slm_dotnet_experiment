using System.Globalization;
using CsvHelper;

namespace DotNetSlmEfficiency;

/// <summary>
/// Logs energy metrics to CSV files
/// </summary>
public static class MetricsLogger
{
    public static void LogMetrics(string fileName, string platform, string model, EnergyMetrics metrics, string responseText)
    {
        var record = new MetricsRecord
        {
            Timestamp = DateTime.UtcNow,
            Platform = platform,
            Model = model,
            DurationSeconds = metrics.DurationSeconds,
            CpuTimeSeconds = metrics.CpuTimeSeconds,
            CpuUsagePercent = metrics.CpuUsagePercent,
            StartMemoryMB = metrics.StartMemoryMB,
            EndMemoryMB = metrics.EndMemoryMB,
            PeakMemoryMB = metrics.PeakMemoryMB,
            MemoryDeltaMB = metrics.MemoryDeltaMB,
            ProcessorCount = metrics.ProcessorCount,
            ResponseLength = responseText.Length,
            ResponsePreview = responseText.Length > 100 ? responseText[..100] : responseText,
            
            // Enhanced metrics
            SystemCpuUsagePercent = metrics.SystemCpuUsagePercent,
            SystemMemoryUsagePercent = metrics.SystemMemoryUsagePercent,
            SystemMemoryUsedMB = metrics.SystemMemoryUsedBytes.HasValue ? metrics.SystemMemoryUsedBytes.Value / (1024.0 * 1024.0) : null,
            GuaranteedMemoryMB = metrics.GuaranteedMemoryBytes.HasValue ? metrics.GuaranteedMemoryBytes.Value / (1024.0 * 1024.0) : null,
            MaximumMemoryMB = metrics.MaximumMemoryBytes.HasValue ? metrics.MaximumMemoryBytes.Value / (1024.0 * 1024.0) : null,
            GuaranteedCpuUnits = metrics.GuaranteedCpuUnits,
            MaximumCpuUnits = metrics.MaximumCpuUnits,
            HasEnhancedMetrics = metrics.HasEnhancedMetrics
        };

        var fileExists = File.Exists(fileName);
        
        using var stream = File.Open(fileName, FileMode.Append, FileAccess.Write);
        using var writer = new StreamWriter(stream);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        
        if (!fileExists)
        {
            csv.WriteHeader<MetricsRecord>();
            csv.NextRecord();
        }
        
        csv.WriteRecord(record);
        csv.NextRecord();

        Console.WriteLine($"\nMetrics logged to: {fileName}");
    }
}

public class MetricsRecord
{
    public DateTime Timestamp { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
    public double CpuTimeSeconds { get; set; }
    public double CpuUsagePercent { get; set; }
    public double StartMemoryMB { get; set; }
    public double EndMemoryMB { get; set; }
    public double PeakMemoryMB { get; set; }
    public double MemoryDeltaMB { get; set; }
    public int ProcessorCount { get; set; }
    public int ResponseLength { get; set; }
    public string ResponsePreview { get; set; } = string.Empty;
    
    // Enhanced Microsoft Resource Monitoring metrics
    public double? SystemCpuUsagePercent { get; set; }
    public double? SystemMemoryUsagePercent { get; set; }
    public double? SystemMemoryUsedMB { get; set; }
    public double? GuaranteedMemoryMB { get; set; }
    public double? MaximumMemoryMB { get; set; }
    public double? GuaranteedCpuUnits { get; set; }
    public double? MaximumCpuUnits { get; set; }
    public bool HasEnhancedMetrics { get; set; }
}
