# BoatSLM_cs_experiment

This repository is used to experiment with Ollama and a custom small language model in the .NET framework. It is also used for demonstration purposes at the .NET Conference Thailand at Seven Peaks Software.

## Project Purpose

The purpose of this project is to demonstrate the integration of Ollama with a custom small language model in the .NET framework. The project includes examples of how to set up and use the language model, as well as how to run the provided code.

## Project Structure

The project is structured as follows:

```
ollama_autogen/
  ├── Chat_With_LLaMA.cs
  ├── Chat_With_LLaVA.cs
  ├── nuget.config
  ├── ollama_autogen.csproj
  ├── ollama_autogen.sln
  └── Program.cs
ollama_semantic/
  ├── nuget.config
  ├── ollama_semantic.csproj
  ├── ollama_semantic.sln
  └── Program.cs
README.md
```

### File Descriptions

- `ollama_autogen/Chat_With_LLaMA.cs`: Example code for creating an Ollama agent and sending a message.
- `ollama_autogen/Chat_With_LLaVA.cs`: Example code for creating an Ollama agent with multi-modal message support.
- `ollama_autogen/nuget.config`: NuGet configuration file for the `ollama_autogen` project.
- `ollama_autogen/ollama_autogen.csproj`: Project file for the `ollama_autogen` project.
- `ollama_autogen/ollama_autogen.sln`: Solution file for the `ollama_autogen` project.
- `ollama_autogen/Program.cs`: Entry point for the `ollama_autogen` project.
- `ollama_semantic/nuget.config`: NuGet configuration file for the `ollama_semantic` project.
- `ollama_semantic/ollama_semantic.csproj`: Project file for the `ollama_semantic` project.
- `ollama_semantic/ollama_semantic.sln`: Solution file for the `ollama_semantic` project.
- `ollama_semantic/Program.cs`: Entry point for the `ollama_semantic` project.
- `README.md`: This file.

## Setup Instructions

### Prerequisites

- .NET SDK 9.0 or later
- NuGet package manager

### Installation

1. Clone the repository:
   ```sh
   git clone https://github.com/chrnthnkmutt/BoatSLM_cs_experiment.git
   cd BoatSLM_cs_experiment
   ```

2. Restore the NuGet packages:
   ```sh
   dotnet restore
   ```

## Usage

### Running the Code

To run the `ollama_autogen` project:
```sh
cd ollama_autogen
dotnet run
```

To run the `ollama_semantic` project:
```sh
cd ollama_semantic
dotnet run
```

### Examples

#### Example 1: Chat with LLaMA

The `Chat_With_LLaMA.cs` file contains an example of how to create an Ollama agent and send a message to it. The agent will respond with a piece of C# code to calculate the 100th Fibonacci number.

#### Example 2: Chat with LLaVA

The `Chat_With_LLaVA.cs` file contains an example of how to create an Ollama agent with multi-modal message support. The agent can process both text and image messages and respond accordingly.

## Additional Configuration

If you need to change the base address of the Ollama agent, you can modify the `BaseAddress` property in the `Chat_With_LLaMA.cs` and `Chat_With_LLaVA.cs` files.

For example:
```csharp
using var httpClient = new HttpClient()
{
    BaseAddress = new Uri("http://localhost:11434"),
};
```
