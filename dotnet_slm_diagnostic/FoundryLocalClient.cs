using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetSlmEfficiency;

/// <summary>
/// Client for testing Foundry Local inference with CPU efficiency monitoring
/// Uses Foundry Local's OpenAI-compatible endpoint
/// Enhanced with Microsoft.Extensions.Diagnostics.ResourceMonitoring
/// </summary>
public class FoundryLocalClient : IDisposable
{
    private const string ModelName = "qwen2.5-0.5b-instruct-generic-cpu:4";
    private const string OutputFile = "foundry_local_cpu_metrics.csv";
    private readonly HttpClient _httpClient;
    private readonly IHost? _resourceHost;
    private readonly IResourceMonitor? _resourceMonitor;

    public FoundryLocalClient()
    {
        var endpoint = Environment.GetEnvironmentVariable("FOUNDRY_ENDPOINT") ?? "http://127.0.0.1:59396";
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(endpoint),
            Timeout = TimeSpan.FromMinutes(5)
        };
        
        // Initialize Microsoft Resource Monitoring for this client
        try
        {
            _resourceHost = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning))
                            .AddResourceMonitoring();
                })
                .Build();
            
            _resourceHost.StartAsync().Wait();
            _resourceMonitor = _resourceHost.Services.GetService<IResourceMonitor>();
            Console.WriteLine("FoundryLocal Client: Enhanced resource monitoring initialized.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FoundryLocal Client: Could not initialize resource monitoring: {ex.Message}");
            _resourceMonitor = null;
            _resourceHost = null;
        }
    }

    public async Task RunInferenceWithMonitoring()
    {
        try
        {
            Console.WriteLine($"Initializing Foundry Local with model: {ModelName}");
            Console.WriteLine("Note: Ensure Foundry Local service is running with the model loaded.");
            Console.WriteLine("Start with: foundry model run " + ModelName);
            Console.WriteLine($"Endpoint: {_httpClient.BaseAddress}\n");

            // Check if Foundry Local is running
            try
            {
                var healthCheck = await _httpClient.GetAsync("/health");
                if (healthCheck.IsSuccessStatusCode)
                {
                    Console.WriteLine("✓ Foundry Local service is running.\n");
                }
                else
                {
                    Console.WriteLine($"⚠️ Foundry Local health check returned: {healthCheck.StatusCode}\n");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"❌ Cannot connect to Foundry Local at {_httpClient.BaseAddress}");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("\nTroubleshooting steps:");
                Console.WriteLine("1. Check if Foundry Local is installed and running");
                Console.WriteLine($"2. Start the model: foundry model run {ModelName}");
                Console.WriteLine("3. Verify the service is accessible");
                Console.WriteLine("4. Check if a firewall is blocking the connection");
                Console.WriteLine($"5. Try setting FOUNDRY_ENDPOINT environment variable if using a different port\n");
                
                throw new Exception($"Foundry Local is not accessible at {_httpClient.BaseAddress}. Please ensure it's running.", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Could not verify Foundry Local service status: {ex.Message}\n");
            }

            var messages = new List<OllamaMessage>
            {
                new() { Role = "system", Content = "You are a helpful assistant." },
                new() { Role = "user", Content = "Who won the world series in 2020?" },
                new() { Role = "assistant", Content = "The LA Dodgers won in 2020." },
                new() { Role = "user", Content = "Where was it played?" }
            };

            // Start both monitoring approaches
            using var monitor = new EnergyMonitor();
            monitor.Start();
            
            // Capture initial resource state with Microsoft monitoring
            ResourceUtilization? initialResourceState = null;
            if (_resourceMonitor != null)
            {
                try
                {
                    initialResourceState = _resourceMonitor.GetUtilization(TimeSpan.FromSeconds(1));
                    if (initialResourceState.HasValue)
                    {
                        var state = initialResourceState.Value;
                        Console.WriteLine($"Initial System CPU: {state.CpuUsedPercentage:F2}%, Memory: {state.MemoryUsedPercentage:F2}%");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not capture initial resource state: {ex.Message}");
                }
            }

            Console.WriteLine("Sending request to Foundry Local...\n");

            var request = new
            {
                model = ModelName,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                stream = false,
                temperature = 1.0f
            };

            var response = await _httpClient.PostAsJsonAsync("/v1/chat/completions", request);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var chatResponse = JsonSerializer.Deserialize<FoundryChatResponse>(jsonResponse);
            
            var responseText = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
            
            Console.WriteLine("Response:");
            Console.WriteLine(responseText);

            // Capture final resource state
            ResourceUtilization? finalResourceState = null;
            if (_resourceMonitor != null)
            {
                try
                {
                    finalResourceState = _resourceMonitor.GetUtilization(TimeSpan.FromSeconds(1));
                    if (finalResourceState.HasValue)
                    {
                        var finalState = finalResourceState.Value;
                        Console.WriteLine($"\nFinal System CPU: {finalState.CpuUsedPercentage:F2}%, Memory: {finalState.MemoryUsedPercentage:F2}%");
                        
                        if (initialResourceState.HasValue)
                        {
                            var initialState = initialResourceState.Value;
                            var cpuDelta = finalState.CpuUsedPercentage - initialState.CpuUsedPercentage;
                            var memoryDelta = finalState.MemoryUsedPercentage - initialState.MemoryUsedPercentage;
                            Console.WriteLine($"Resource Deltas - CPU: {cpuDelta:+0.00;-0.00}%, Memory: {memoryDelta:+0.00;-0.00}%");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not capture final resource state: {ex.Message}");
                }
            }

            monitor.UpdatePeakMemory();
            var metrics = monitor.Stop();

            MetricsLogger.LogMetrics(OutputFile, "FoundryLocal", ModelName, metrics, responseText);
            
            Console.WriteLine("\n✓ Foundry Local test completed successfully.");
            Console.WriteLine("Process fully stopped and metrics recorded.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError running Foundry Local test: {ex.Message}");
            Console.WriteLine("\nTroubleshooting:");
            Console.WriteLine("1. Start Foundry Local: foundry model run " + ModelName);
            Console.WriteLine("2. Verify service is running: foundry service status");
            Console.WriteLine($"3. Check endpoint matches: {_httpClient.BaseAddress}");
            Console.WriteLine($"\nFull error: {ex}");
            Console.WriteLine("\n✗ Foundry Local test failed and stopped.");
        }
    }
    
    public void Dispose()
    {
        try
        {
            _resourceHost?.StopAsync(TimeSpan.FromSeconds(2)).Wait();
            _resourceHost?.Dispose();
            _httpClient?.Dispose();
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
            Console.WriteLine($"Warning during FoundryLocalClient cleanup: {ex.Message}");
        }
        GC.SuppressFinalize(this);
    }
}

// Foundry Local API models (OpenAI-compatible)
public class FoundryChatResponse
{
    [JsonPropertyName("choices")]
    public List<FoundryChoice>? Choices { get; set; }
}

public class FoundryChoice
{
    [JsonPropertyName("message")]
    public OllamaMessage? Message { get; set; }
}
