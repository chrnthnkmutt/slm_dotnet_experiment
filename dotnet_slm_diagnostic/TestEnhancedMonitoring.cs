using DotNetSlmEfficiency;

namespace DotNetSlmEfficiency;

/// <summary>
/// Simple test class to demonstrate the enhanced monitoring capabilities
/// </summary>
public static class TestEnhancedMonitoring
{
    public static async Task RunTest()
    {
        Console.WriteLine("=== Enhanced Monitoring Test ===\n");
        
        using var monitor = new EnergyMonitor();
        
        Console.WriteLine("Starting enhanced monitoring test...");
        monitor.Start();
        
        // Simulate some work
        Console.WriteLine("Performing CPU-intensive task...");
        var sum = 0.0;
        for (int i = 0; i < 10_000_000; i++)
        {
            sum += Math.Sqrt(i);
            if (i % 2_000_000 == 0)
            {
                monitor.UpdatePeakMemory();
                Console.WriteLine($"Progress: {i / 100_000}%");
            }
        }
        
        Console.WriteLine($"Calculated sum: {sum:E2}");
        
        // Allocate some memory to see memory tracking
        Console.WriteLine("Allocating memory...");
        var largeArray = new double[1_000_000];
        var random = new Random();
        for (int i = 0; i < largeArray.Length; i++)
        {
            largeArray[i] = random.NextDouble();
        }
        
        monitor.UpdatePeakMemory();
        
        // Small delay to let monitoring capture the state
        await Task.Delay(1000);
        
        var metrics = monitor.Stop();
        
        // Log to CSV
        MetricsLogger.LogMetrics("enhanced_monitoring_test.csv", "TestPlatform", "TestModel", metrics, $"Test completed with sum: {sum:E2}");
        
        Console.WriteLine("\n=== Test completed successfully! ===");
        Console.WriteLine("Check 'enhanced_monitoring_test.csv' for detailed metrics including:");
        Console.WriteLine("- Process-level CPU and memory metrics");
        if (metrics.HasEnhancedMetrics)
        {
            Console.WriteLine("- System-level CPU and memory utilization percentages");
            Console.WriteLine("- System resource limits and guarantees");
        }
        else
        {
            Console.WriteLine("- Enhanced metrics were not available (normal on some systems)");
        }
    }
}