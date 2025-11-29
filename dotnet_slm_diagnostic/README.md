# .NET SLM CPU Efficiency Comparison

A .NET-based benchmarking suite to compare CPU inference efficiency between **Foundry Local** and **Ollama** for Small Language Models (SLMs).

## Overview

This project measures and compares:
- CPU usage and time
- Memory consumption
- Inference duration
- Response quality

## Prerequisites

### 1. .NET 9 SDK
```bash
# Check if installed
dotnet --version
```

### 2. Foundry Local
- Download: https://aka.ms/foundry-local
- Install and ensure service is running
- The app will use the local endpoint

### 3. Ollama
```bash
# Install from https://ollama.ai
# Then pull the model:
ollama pull qwen2.5:1.5b

# Verify Ollama is running:
ollama list
```

## Installation

```bash
# Restore NuGet packages
dotnet restore

# Build the project
dotnet build
```

## Usage

### Run the application:
```bash
dotnet run
```

### Choose test mode:
1. **Foundry Local only** - Test Microsoft's Foundry Local
2. **Ollama only** - Test Ollama
3. **Run Both** - Compare both platforms sequentially

## Output

The tool generates CSV files with detailed metrics:
- `foundry_local_cpu_metrics.csv` - Foundry Local results
- `ollama_cpu_metrics.csv` - Ollama results

### Metrics Captured:
- Timestamp
- Platform (FoundryLocal/Ollama)
- Model name
- Duration (seconds)
- CPU time (seconds)
- CPU usage (%)
- Memory usage (start, end, peak, delta in MB)
- Processor count
- Response length and preview

## Project Structure

```
├── DotNetSlmEfficiency.csproj  # Project configuration
├── Program.cs                   # Entry point and menu
├── FoundryLocalClient.cs        # Foundry Local integration
├── OllamaClient.cs              # Ollama integration
├── EnergyMonitor.cs             # CPU/Memory monitoring
├── MetricsLogger.cs             # CSV logging
└── README.md                    # This file
```

## Models Used

- **Foundry Local**: qwen2.5-1.5b-instruct-generic-cpu
- **Ollama**: qwen2.5:1.5b (CPU-only mode)

Both use the Qwen 2.5 1.5B parameter model optimized for CPU inference.

## Configuration

### Environment Variables (Optional):
```bash
# For Foundry Local custom endpoint
set FOUNDRY_LOCAL_ENDPOINT=http://localhost:random/v1/chat/completions
set FOUNDRY_LOCAL_API_KEY=your-key-here
```

## Troubleshooting

### Foundry Local Issues:
1. Ensure Foundry Local is installed and running
2. Check the service status in Task Manager
3. Verify the endpoint URL is correct

### Ollama Issues:
1. Ensure Ollama service is running
2. Pull the model: `ollama pull qwen2.5:1.5b`
3. Check Ollama is accessible: `curl http://localhost:11434`

## Analysis

After running tests, you can analyze the CSV files to compare:
- Which platform uses less CPU
- Memory efficiency differences
- Inference speed comparison
- Response consistency

## Future Enhancements

- [ ] Add streaming response support
- [ ] Support for more models
- [ ] GPU efficiency comparison
- [ ] Real-time power consumption monitoring
- [ ] Automated comparison reports
- [ ] Docker containerization

## License

MIT License - Feel free to use and modify for your research.
