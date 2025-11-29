using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetSlmEfficiency;

/// <summary>
/// Enhanced monitors CPU and memory usage during inference operations
/// Combines Process-level monitoring with Microsoft.Extensions.Diagnostics.ResourceMonitoring
/// </summary>
public class EnergyMonitor : IDisposable
{
    private readonly Process _currentProcess;
    private readonly IHost? _host;
    private readonly IResourceMonitor? _resourceMonitor;
    private DateTime _startTime;
    private TimeSpan _startCpuTime;
    private long _startMemory;
    private double _peakMemoryMB;
    private ResourceUtilization? _startResourceUtilization;
    private bool _disposed = false;
    
    public EnergyMonitor()
    {
        _currentProcess = Process.GetCurrentProcess();
        
        // Initialize Microsoft Resource Monitoring
        try
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning))
                            .AddResourceMonitoring();
                })
                .Build();
            
            _host.StartAsync().Wait();
            _resourceMonitor = _host.Services.GetService<IResourceMonitor>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not initialize Microsoft Resource Monitoring: {ex.Message}");
            Console.WriteLine("Continuing with basic Process monitoring only.");
            _resourceMonitor = null;
        }
    }

    public void Start()
    {
        _startTime = DateTime.UtcNow;
        _startCpuTime = _currentProcess.TotalProcessorTime;
        _startMemory = _currentProcess.WorkingSet64;
        _peakMemoryMB = _startMemory / (1024.0 * 1024.0);
        
        // Capture initial resource utilization if available
        if (_resourceMonitor != null)
        {
            try
            {
                _startResourceUtilization = _resourceMonitor.GetUtilization(TimeSpan.FromSeconds(1));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not get initial resource utilization: {ex.Message}");
            }
        }
        
        string monitoringType = _resourceMonitor != null ? "Enhanced" : "Basic Process";
        Console.WriteLine($"Started {monitoringType} CPU and memory monitoring...");
    }

    public EnergyMetrics Stop()
    {
        var endTime = DateTime.UtcNow;
        var endCpuTime = _currentProcess.TotalProcessorTime;
        var endMemory = _currentProcess.WorkingSet64;
        
        var duration = endTime - _startTime;
        var cpuTime = endCpuTime - _startCpuTime;
        var processCpuUsagePercent = (cpuTime.TotalMilliseconds / duration.TotalMilliseconds) * 100.0 / Environment.ProcessorCount;
        
        var currentMemoryMB = endMemory / (1024.0 * 1024.0);
        if (currentMemoryMB > _peakMemoryMB)
        {
            _peakMemoryMB = currentMemoryMB;
        }

        // Get enhanced metrics if available
        ResourceUtilization? endResourceUtilization = null;
        if (_resourceMonitor != null)
        {
            try
            {
                endResourceUtilization = _resourceMonitor.GetUtilization(duration > TimeSpan.FromSeconds(1) ? duration : TimeSpan.FromSeconds(1));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not get final resource utilization: {ex.Message}");
            }
        }

        var metrics = new EnergyMetrics
        {
            // Original Process-based metrics
            DurationSeconds = duration.TotalSeconds,
            CpuTimeSeconds = cpuTime.TotalSeconds,
            CpuUsagePercent = processCpuUsagePercent,
            StartMemoryMB = _startMemory / (1024.0 * 1024.0),
            EndMemoryMB = currentMemoryMB,
            PeakMemoryMB = _peakMemoryMB,
            MemoryDeltaMB = currentMemoryMB - (_startMemory / (1024.0 * 1024.0)),
            ProcessorCount = Environment.ProcessorCount,
            
            // Enhanced Microsoft Resource Monitoring metrics
            SystemCpuUsagePercent = endResourceUtilization?.CpuUsedPercentage,
            SystemMemoryUsagePercent = endResourceUtilization?.MemoryUsedPercentage,
            SystemMemoryUsedBytes = endResourceUtilization?.MemoryUsedInBytes,
            GuaranteedMemoryBytes = endResourceUtilization?.SystemResources.GuaranteedMemoryInBytes,
            MaximumMemoryBytes = endResourceUtilization?.SystemResources.MaximumMemoryInBytes,
            GuaranteedCpuUnits = endResourceUtilization?.SystemResources.GuaranteedCpuUnits,
            MaximumCpuUnits = endResourceUtilization?.SystemResources.MaximumCpuUnits,
            HasEnhancedMetrics = endResourceUtilization != null
        };

        Console.WriteLine("\nStopped monitoring.");
        Console.WriteLine($"Duration: {metrics.DurationSeconds:F2}s");
        Console.WriteLine($"Process CPU Time: {metrics.CpuTimeSeconds:F2}s");
        Console.WriteLine($"Process CPU Usage: {metrics.CpuUsagePercent:F2}%");
        Console.WriteLine($"Memory Delta: {metrics.MemoryDeltaMB:F2} MB");
        Console.WriteLine($"Peak Memory: {metrics.PeakMemoryMB:F2} MB");
        
        if (metrics.HasEnhancedMetrics)
        {
            Console.WriteLine($"System CPU Usage: {metrics.SystemCpuUsagePercent:F2}%");
            Console.WriteLine($"System Memory Usage: {metrics.SystemMemoryUsagePercent:F2}%");
            if (metrics.MaximumMemoryBytes.HasValue && metrics.MaximumMemoryBytes > 0)
            {
                Console.WriteLine($"Memory Limits: {metrics.GuaranteedMemoryBytes / (1024.0 * 1024.0):F0} MB guaranteed / {metrics.MaximumMemoryBytes / (1024.0 * 1024.0):F0} MB maximum");
            }
        }

        return metrics;
    }

    public void UpdatePeakMemory()
    {
        var currentMemoryMB = _currentProcess.WorkingSet64 / (1024.0 * 1024.0);
        if (currentMemoryMB > _peakMemoryMB)
        {
            _peakMemoryMB = currentMemoryMB;
        }
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _host?.StopAsync(TimeSpan.FromSeconds(2)).Wait();
                _host?.Dispose();
            }
            catch (TaskCanceledException)
            {
                // Expected when background services are canceled during shutdown - suppress this
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException))
            {
                // Expected when background services are canceled during shutdown - suppress this
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning during cleanup: {ex.Message}");
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

public class EnergyMetrics
{
    // Original Process-based metrics
    public double DurationSeconds { get; set; }
    public double CpuTimeSeconds { get; set; }
    public double CpuUsagePercent { get; set; }
    public double StartMemoryMB { get; set; }
    public double EndMemoryMB { get; set; }
    public double PeakMemoryMB { get; set; }
    public double MemoryDeltaMB { get; set; }
    public int ProcessorCount { get; set; }
    
    // Enhanced Microsoft Resource Monitoring metrics
    public double? SystemCpuUsagePercent { get; set; }
    public double? SystemMemoryUsagePercent { get; set; }
    public ulong? SystemMemoryUsedBytes { get; set; }
    public ulong? GuaranteedMemoryBytes { get; set; }
    public ulong? MaximumMemoryBytes { get; set; }
    public double? GuaranteedCpuUnits { get; set; }
    public double? MaximumCpuUnits { get; set; }
    public bool HasEnhancedMetrics { get; set; }
}
