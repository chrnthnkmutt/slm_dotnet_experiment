# slm_dotnet_experiment

Created By Charunthon Limseelo - Microsoft Learn Student Ambassador, Thailand
For more information about the author, please click to his [official website.](https://chrnthnkmutt.github.io)

## Project Purpose

The purpose of this project is to demonstrate the integration of Ollama or LM Studio with a custom small language models, small reasoning models, or any local models in the .NET framework for making AI agents. The project includes examples of how to set up and use the language model, as well as how to run the provided code. In meantime, this repository is been using for the demonstration in many events as follows:

- .NET Conf Thailand 2024, on November 23th 2024 at Seven Peaks Software
- .NET Developer Day 2025, on March 8th 2025 at Agoda Thailand, centralwOrld offices
- Operation on Local AI Agent: Test Drive on Deepseek-R1 Reasoning and AutoGen with .NET and Ollama ([Watch the video here](https://chrnthnkmutt.github.io/talks/deepseekr1))

The updated slide of presenting at the event will be published and download it within this repository.

## Project Structure

The project is structured as follows:

```
Autogen_LMS/
  ├── Autogen_MLS.csproj
  ├── Autogen_MLS.sln
  ├── nuget.config
  └── Program.cs
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

### Folder's Descriptions

1. AutoGen_LMS: This folder is using for making the experiment of using language model with the LM Studio. For loading or calling the model, we do not need to change the model name in the Program.cs file. However, for LM Studio, we are using on LMS CLI for loading model instead and deploy from CLI or UI app to another project, also with this folder.

2. Ollama_Autogen: This folder is using for making the experiment using language model with OLlama, along with making the agents with AutoGen. However, you can select between text agent or multimodal (image) agent to run. Before running the program, please make sure that your local Ollama server is open by checking with `ollama serve`.

3. Ollama_Semantic: This folder is created for running with using Semantic Kernel, and the setting of Ollama is same as the above one.

*For those are using in Ollama folders, please remind to yourself that you might need to change the model name before running the program*

## Setup Instructions

### Prerequisites

- .NET SDK 9.0 or later
- NuGet package manager
- AutoGen Packages of Each Folder (If it is Ollama, install AutoGen.Ollama. If it is LM Studio, install AutoGen.LMStudio. Both of them requires version number - please check it in the NuGet website.)

### Installation

1. Clone the repository:
   ```sh
   git clone https://github.com/chrnthnkmutt/BoatSLM_cs_experiment.git
   cd slm_dotnet_experiment
   ```

2. Restore the NuGet packages:
   ```sh
   dotnet restore
   ```
* For those who would like to create project with `dotnet new console`, please remind yourself to run `nuget new cofig` first before installing AutoGen or other packages.

## Usage

1. Get into the project:
  ```sh
  cd <project folder>
  ```

2. Run the program:
  ```sh
  dotnet run Program.cs
  ```

Have fun with developing AI apps!!