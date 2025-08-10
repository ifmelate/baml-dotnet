# BAML .NET Client Library

A comprehensive .NET client library for BAML (Basically a Made-up Language) that uses C# Source Generators to provide type-safe, compile-time code generation for AI workflows and agents.

## Overview

This library brings the power of BAML to the .NET ecosystem, allowing developers to define AI functions in BAML schema files and automatically generate strongly-typed C# client code. The library provides both synchronous and asynchronous APIs, with full support for streaming responses.

## Features

- **Type-Safe Code Generation**: Automatically generates C# types and client methods from BAML schema files
- **Source Generators**: Compile-time code generation with no runtime reflection
- **Async/Await Support**: Modern .NET async patterns throughout
- **Streaming Support**: Real-time streaming responses using `IAsyncEnumerable<T>`
- **Error Handling**: Comprehensive exception handling with BAML-specific error types
- **Configurable Runtime**: Flexible configuration for different BAML endpoints and authentication
- **MSBuild Integration**: Seamless integration with .NET build process

## Quick Start

### 1. Installation

Add the BAML libraries to your project:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/Baml.Runtime/Baml.Runtime.csproj" />
  <Analyzer Include="path/to/Baml.SourceGenerator/bin/Debug/netstandard2.0/Baml.SourceGenerator.dll" />
</ItemGroup>
```

### 2. Define BAML Schema

Create a `.baml` file in your project (e.g., `chat_agent.baml`):

```baml
class Message {
    role string
    content string
}

class ReplyTool {
    response string
}

function ChatAgent(messages: Message[], tone: "happy" | "sad") -> ReplyTool {
    client "openai/gpt-4o-mini"
    
    prompt #"
        Be a {{ tone }} bot.
        {{ ctx.output_format }}
        {% for m in messages %}
        {{ _.role(m.role) }}
        {{ m.content }}
        {% endfor %}
    "#
}
```

### 3. Include BAML File in Project

Add the BAML file to your project:

```xml
<ItemGroup>
  <AdditionalFiles Include="chat_agent.baml" />
</ItemGroup>
```

### 4. Use Generated Client

```csharp
using Baml.Runtime;
using Baml.Generated;

// Configure the runtime
var configuration = new BamlConfiguration
{
    ApiEndpoint = "http://localhost:8000/api/baml/call",
    StreamingEndpoint = "http://localhost:8000/api/baml/stream",
    ApiKey = Environment.GetEnvironmentVariable("BAML_API_KEY")
};

// Create client
using var runtime = new BamlRuntime(configuration);
var client = new BamlGeneratedClient(runtime);

// Call BAML function
var messages = new List<Message>
{
    new Message { Role = "user", Content = "Hello!" }
};

var response = await client.ChatAgentAsync(messages, "happy");
Console.WriteLine(response.Response);
```

## Architecture

### Components

1. **Baml.Runtime**: Core runtime library for HTTP communication and serialization
2. **Baml.SourceGenerator**: C# Source Generator that parses BAML files and generates client code
3. **Generated Code**: Type-safe client interfaces and implementations

### Code Generation Process

1. **Build Time**: Source generator discovers `.baml` files in the project
2. **Parsing**: BAML files are parsed to extract functions, classes, and enums
3. **Generation**: C# code is generated for types and client methods
4. **Compilation**: Generated code is compiled alongside your application code

## Examples

### Basic Function Call

```csharp
// Simple text extraction
var result = await client.ExtractInfoAsync(
    "John Doe is a software engineer at Microsoft.");
Console.WriteLine($"Extracted: {result}");
```

### Streaming Response

```csharp
// Streaming chat
await foreach (var chunk in client.StreamChatAgentAsync(messages, "happy"))
{
    Console.Write(chunk.Response);
}
```

### Error Handling

```csharp
try
{
    var result = await client.ChatAgentAsync(messages, "happy");
    // Handle success
}
catch (BamlFunctionException ex)
{
    Console.WriteLine($"BAML function '{ex.FunctionName}' failed: {ex.Message}");
}
catch (BamlConfigurationException ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
}
```

## Configuration

### BamlConfiguration Options

```csharp
var configuration = new BamlConfiguration
{
    ApiEndpoint = "http://localhost:8000/api/baml/call",      // Required
    StreamingEndpoint = "http://localhost:8000/api/baml/stream", // Required for streaming
    ApiKey = "your-api-key",                                  // Optional
    Timeout = TimeSpan.FromMinutes(5)                         // Optional, default 5 minutes
};
```

### Environment Variables

The library supports configuration via environment variables:

- `BAML_API_KEY`: API key for authentication
- `BAML_API_ENDPOINT`: Override default API endpoint
- `BAML_STREAMING_ENDPOINT`: Override default streaming endpoint

## BAML Schema Support

### Supported Types

- **Primitives**: `string`, `int`, `float`, `double`, `bool`
- **Arrays**: `Type[]` (mapped to `IEnumerable<Type>`)
- **Custom Classes**: User-defined classes with properties
- **Enums**: String-based enumerations
- **Union Types**: `Type1 | Type2` (mapped to `object`)

### Function Definitions

```baml
function FunctionName(param1: Type1, param2: Type2) -> ReturnType {
    client "model-provider/model-name"
    prompt #"Your prompt template here"#
}
```

### Class Definitions

```baml
class ClassName {
    property1 string
    property2 int
    property3 CustomType @description("Optional description")
}
```

### Enum Definitions

```baml
enum StatusType {
    "active"
    "inactive"
    "pending"
}
```

## Advanced Usage

### Custom HTTP Client

```csharp
using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("Custom-Header", "value");

var runtime = new BamlRuntime(configuration, httpClient);
```

### Dependency Injection

```csharp
// In Startup.cs or Program.cs
services.AddSingleton<BamlConfiguration>(provider => new BamlConfiguration
{
    ApiEndpoint = "http://localhost:8000/api/baml/call",
    ApiKey = Environment.GetEnvironmentVariable("BAML_API_KEY")
});

services.AddScoped<IBamlRuntime, BamlRuntime>();
services.AddScoped<IBamlGeneratedClient, BamlGeneratedClient>();
```

## Building from Source

### Prerequisites

- .NET 8.0 SDK or later
- Git

### Build Steps

```bash
git clone <repository-url>
cd baml-dotnet
dotnet restore
dotnet build
```

### Running Examples

```bash
# Simple example
cd examples/SimpleExample
dotnet run

# Streaming example
cd examples/StreamingExample
dotnet run
```

### Running Tests

```bash
dotnet test
```

## Project Structure

```
baml-dotnet/
├── src/
│   ├── Baml.Runtime/           # Core runtime library
│   └── Baml.SourceGenerator/   # Source generator
├── examples/
│   ├── SimpleExample/          # Basic usage example
│   └── StreamingExample/       # Streaming example
├── tests/
│   ├── Baml.Runtime.Tests/
│   └── Baml.SourceGenerator.Tests/
└── README.md
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## License

This project is licensed under the Apache 2.0 License - see the LICENSE file for details.

## Acknowledgments

- [BAML Project](https://github.com/BoundaryML/baml) - The original BAML language and runtime
- [Microsoft Roslyn](https://github.com/dotnet/roslyn) - C# Source Generators infrastructure

## Support

For issues and questions:

1. Check the [examples](examples/) for common usage patterns
2. Review the [BAML documentation](https://docs.boundaryml.com/)
3. Open an issue on GitHub

## Roadmap

- [ ] NuGet package distribution
- [ ] Additional BAML language features
- [ ] Performance optimizations
- [ ] Integration with popular .NET frameworks (ASP.NET Core, Blazor)
- [ ] Visual Studio extension for BAML syntax highlighting

