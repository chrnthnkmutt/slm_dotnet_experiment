using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotNetSlmEfficiency;

/// <summary>
/// Client for testing Ollama inference with CPU efficiency monitoring
/// Enhanced with Microsoft.Extensions.Diagnostics.ResourceMonitoring
/// </summary>
public class OllamaClient : IDisposable
{
    private const string ModelName = "qwen_cpu:latest";
    private const string OutputFile = "ollama_cpu_metrics.csv";
    private readonly HttpClient _httpClient;
    private readonly IHost? _resourceHost;
    private readonly IResourceMonitor? _resourceMonitor;

    public OllamaClient()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:11434"),
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
            Console.WriteLine("Ollama Client: Enhanced resource monitoring initialized.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ollama Client: Could not initialize resource monitoring: {ex.Message}");
            _resourceMonitor = null;
            _resourceHost = null;
        }
    }

    public async Task RunInferenceWithMonitoring()
    {
        try
        {
            Console.WriteLine($"Initializing Ollama with model: {ModelName}");
            Console.WriteLine("Note: Ensure Ollama is installed and running.");
            Console.WriteLine("Install from: https://ollama.ai\n");

            // Check if Ollama is running
            try
            {
                var healthCheck = await _httpClient.GetAsync("/");
                Console.WriteLine("Ollama service is running.\n");
            }
            catch
            {
                throw new Exception("Cannot connect to Ollama. Ensure it's running on localhost:11434");
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

            Console.WriteLine("Sending request to Ollama...\n");

            var request = new OllamaChatRequest
            {
                Model = ModelName,
                Messages = messages,
                Stream = false,
                Options = new OllamaOptions
                {
                    Temperature = 1.0f,
                    NumGpu = 0  // Force CPU-only inference
                }
            };

            var response = await _httpClient.PostAsJsonAsync("/api/chat", request);
            response.EnsureSuccessStatusCode();

            var chatResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>();
            
            var responseText = chatResponse?.Message?.Content ?? string.Empty;
            
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

            MetricsLogger.LogMetrics(OutputFile, "Ollama", ModelName, metrics, responseText);
            
            Console.WriteLine("\n✓ Ollama test completed successfully.");
            Console.WriteLine("Process fully stopped and metrics recorded.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError running Ollama test: {ex.Message}");
            Console.WriteLine("\nTroubleshooting:");
            Console.WriteLine("1. Install Ollama from https://ollama.ai");
            Console.WriteLine("2. Run: ollama pull qwen2.5:1.5b");
            Console.WriteLine("3. Ensure Ollama service is running");
            Console.WriteLine($"\nFull error: {ex}");
            Console.WriteLine("\n✗ Ollama test failed and stopped.");
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
            Console.WriteLine($"Warning during OllamaClient cleanup: {ex.Message}");
        }
        GC.SuppressFinalize(this);
    }
}

// Ollama API models
public class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OllamaMessage> Messages { get; set; } = new();

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("options")]
    public OllamaOptions? Options { get; set; }
}

public class OllamaMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class OllamaOptions
{
    [JsonPropertyName("temperature")]
    public float Temperature { get; set; }

    [JsonPropertyName("num_gpu")]
    public int NumGpu { get; set; }
}

public class OllamaChatResponse
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("message")]
    public OllamaMessage? Message { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }
}
