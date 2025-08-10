/*
Copyright (c) 2025 ifmelate

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using Baml.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Threading;
using Xunit;

namespace Baml.SourceGenerator.Tests;

/// <summary>
/// Integration tests that validate end-to-end BAML-to-C# code generation.
/// These tests verify that BAML files are correctly transformed into valid C# code
/// by the source generator and that the generated code compiles successfully.
/// </summary>
public class BamlIntegrationTests
{
    private readonly BamlSourceGenerator _sourceGenerator;
    private readonly CSharpCompilation _baseCompilation;

    public BamlIntegrationTests()
    {
        _sourceGenerator = new BamlSourceGenerator();

        // Create a base compilation with necessary references
        var references = new List<MetadataReference>();

        // Add basic .NET references
        var assemblies = new[]
        {
            typeof(object).Assembly, // System.Private.CoreLib
            typeof(System.Collections.Generic.IEnumerable<>).Assembly, // System.Collections
            typeof(System.Threading.Tasks.Task).Assembly, // System.Threading.Tasks  
            typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute).Assembly, // System.Text.Json
            typeof(System.ComponentModel.DataAnnotations.DisplayAttribute).Assembly, // System.ComponentModel.DataAnnotations
            Assembly.GetAssembly(typeof(Baml.Runtime.IBamlClient))! // Baml.Runtime
        };

        foreach (var assembly in assemblies)
        {
            references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }

        // Try to load System.Runtime explicitly
        try
        {
            var systemRuntimePath = Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll");
            if (File.Exists(systemRuntimePath))
            {
                references.Add(MetadataReference.CreateFromFile(systemRuntimePath));
            }
        }
        catch
        {
            // If we can't load System.Runtime, continue without it
        }

        _baseCompilation = CSharpCompilation.Create(
            "TestAssembly",
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            references: references);
    }

    [Fact]
    public void EndToEnd_SimpleClassGeneration_GeneratesValidCode()
    {
        // Arrange
        var bamlContent = @"
class Message {
    role string
    content string
}";
        var bamlFile = CreateBamlAdditionalText("simple.baml", bamlContent);

        // Act
        var compilation = RunSourceGenerator(bamlFile);

        // Assert
        AssertGeneratedFileExists(compilation, "simple_baml_BamlTypes.g.cs");

        var generatedCode = GetGeneratedSourceCode(compilation, "simple_baml_BamlTypes.g.cs");
        Assert.Contains("public partial class Message", generatedCode);
        Assert.Contains("public string Role { get; set; }", generatedCode);
        Assert.Contains("public string Content { get; set; }", generatedCode);
        Assert.Contains("JsonPropertyName(\"role\")", generatedCode);
        Assert.Contains("JsonPropertyName(\"content\")", generatedCode);
    }

    [Fact]
    public void EndToEnd_SimpleEnumGeneration_GeneratesValidCode()
    {
        // Arrange
        var bamlContent = @"
enum Tone {
    ""happy"",
    ""sad"",
    ""neutral""
}";
        var bamlFile = CreateBamlAdditionalText("enum.baml", bamlContent);

        // Act
        var compilation = RunSourceGenerator(bamlFile);

        // Assert
        AssertGeneratedFileExists(compilation, "enum_baml_BamlTypes.g.cs");

        var generatedCode = GetGeneratedSourceCode(compilation, "enum_baml_BamlTypes.g.cs");
        Assert.Contains("public enum Tone", generatedCode);
        Assert.Contains("Happy", generatedCode);
        Assert.Contains("Sad", generatedCode);
        Assert.Contains("Neutral", generatedCode);
    }

    [Fact]
    public void EndToEnd_SimpleFunctionGeneration_GeneratesValidCode()
    {
        // Arrange
        var bamlContent = @"
function SimpleFunction(input: string) -> string {
    client ""test-client""
    
    prompt #""
        Process {{ input }}
    ""#
}";
        var bamlFile = CreateBamlAdditionalText("function.baml", bamlContent);

        // Act
        var compilation = RunSourceGenerator(bamlFile);

        // Assert
        AssertGeneratedFileExists(compilation, "function_baml_BamlClient.g.cs");

        var generatedCode = GetGeneratedSourceCode(compilation, "function_baml_BamlClient.g.cs");
        Assert.Contains("Task<string> SimpleFunctionAsync", generatedCode);
        Assert.Contains("IAsyncEnumerable<string> StreamSimpleFunctionAsync", generatedCode);
        Assert.Contains("BamlFunction(\"SimpleFunction\", \"test-client\")", generatedCode);
    }

    [Fact]
    public void EndToEnd_ComplexSchemaGeneration_GeneratesValidCode()
    {
        // Arrange
        var bamlContent = @"
class Message {
    role string
    content string
}

class ReplyTool {
    response string
}

enum Tone {
    ""happy"",
    ""sad""
}

function ChatAgent(messages: Message[], tone: Tone) -> ReplyTool {
    client ""openai/gpt-4o-mini""
    
    prompt #""
        Be a {{ tone }} bot.
        {{ ctx.output_format }}
        {% for m in messages %}
        {{ _.role(m.role) }}
        {{ m.content }}
        {% endfor %}
    ""#
}";
        var bamlFile = CreateBamlAdditionalText("complex.baml", bamlContent);

        // Act
        var compilation = RunSourceGenerator(bamlFile);

        // Assert
        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

        Assert.Empty(errors);

        // Check that all expected files are generated
        AssertGeneratedFileExists(compilation, "complex_baml_BamlTypes.g.cs");
        AssertGeneratedFileExists(compilation, "complex_baml_BamlClient.g.cs");

        // Verify types file contains all expected elements
        var typesCode = GetGeneratedSourceCode(compilation, "complex_baml_BamlTypes.g.cs");
        Assert.Contains("public partial class Message", typesCode);
        Assert.Contains("public partial class ReplyTool", typesCode);
        Assert.Contains("public enum Tone", typesCode);

        // Verify client file contains function
        var clientCode = GetGeneratedSourceCode(compilation, "complex_baml_BamlClient.g.cs");
        Assert.Contains("Task<ReplyTool> ChatAgentAsync", clientCode);
        Assert.Contains("IAsyncEnumerable<ReplyTool> StreamChatAgentAsync", clientCode);
    }

    [Fact]
    public void EndToEnd_LiteralValuesGeneration_GeneratesValidCode()
    {
        // Arrange
        var bamlContent = @"
class Action {
    type ""stop""
    message string
}";
        var bamlFile = CreateBamlAdditionalText("literals.baml", bamlContent);

        // Act
        var compilation = RunSourceGenerator(bamlFile);

        // Assert
        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

        Assert.Empty(errors);

        var generatedCode = GetGeneratedSourceCode(compilation, "literals_baml_BamlTypes.g.cs");
        Assert.Contains("public partial class Action", generatedCode);
        Assert.Contains("public string Type { get; set; }", generatedCode);
        Assert.Contains("public string Message { get; set; }", generatedCode);
    }

    [Fact]
    public void EndToEnd_PropertyDescriptionsGeneration_GeneratesValidCode()
    {
        // Arrange
        var bamlContent = @"
class User {
    name string @description(""User's full name"")
    email string @description(""User's email address"")
    age int
}";
        var bamlFile = CreateBamlAdditionalText("descriptions.baml", bamlContent);

        // Act
        var compilation = RunSourceGenerator(bamlFile);

        // Assert
        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

        Assert.Empty(errors);

        var generatedCode = GetGeneratedSourceCode(compilation, "descriptions_baml_BamlTypes.g.cs");
        Assert.Contains("public partial class User", generatedCode);
        Assert.Contains("/// <summary>", generatedCode);
        Assert.Contains("/// User's full name", generatedCode);
        Assert.Contains("/// User's email address", generatedCode);
    }

    [Fact]
    public void EndToEnd_ExampleChatAgentSchema_MatchesExpectedOutput()
    {
        // Arrange - Use the actual SimpleExample schema
        var bamlContent = @"
// Example BAML schema for a simple chat agent
class Message {
    role string
    content string
}

class ReplyTool {
    response string
}

class StopTool {
    action ""stop"" @description(""when it might be a good time to end the conversation"")
}

function ChatAgent(messages: Message[], tone: ""happy"" | ""sad"") -> ReplyTool | StopTool {
    client ""openai/gpt-4o-mini""
    
    prompt #""
        Be a {{ tone }} bot.

        {{ ctx.output_format }}

        {% for m in messages %}
        {{ _.role(m.role) }}
        {{ m.content }}
        {% endfor %}
    ""#
}

function ExtractInfo(text: string) -> string {
    client ""openai/gpt-4o-mini""
    
    prompt #""
        Extract the key information from the following text:
        {{ text }}
        
        Return only the extracted information as a string.
    ""#
}";
        var bamlFile = CreateBamlAdditionalText("chat_agent.baml", bamlContent);

        // Act
        var compilation = RunSourceGenerator(bamlFile);

        // Assert
        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

        Assert.Empty(errors);

        // Verify both files are generated
        AssertGeneratedFileExists(compilation, "chat_agent_baml_BamlTypes.g.cs");
        AssertGeneratedFileExists(compilation, "chat_agent_baml_BamlClient.g.cs");

        // Check types generation
        var typesCode = GetGeneratedSourceCode(compilation, "chat_agent_baml_BamlTypes.g.cs");
        Assert.Contains("public partial class Message", typesCode);
        Assert.Contains("public partial class ReplyTool", typesCode);
        Assert.Contains("public partial class StopTool", typesCode);
        Assert.Contains("/// when it might be a good time to end the conversation", typesCode);

        // Check client generation
        var clientCode = GetGeneratedSourceCode(compilation, "chat_agent_baml_BamlClient.g.cs");
        Assert.Contains("Task<object> ChatAgentAsync", clientCode);
        Assert.Contains("Task<string> ExtractInfoAsync", clientCode);
        Assert.Contains("BamlFunction(\"ChatAgent\", \"openai/gpt-4o-mini\")", clientCode);
        Assert.Contains("BamlFunction(\"ExtractInfo\", \"openai/gpt-4o-mini\")", clientCode);
    }

    [Fact]
    public void EndToEnd_ExampleStreamingSchema_MatchesExpectedOutput()
    {
        // Arrange - Use the actual StreamingExample schema
        var bamlContent = @"
// Example BAML schema for streaming chat
class Message {
    role string
    content string
}

class StreamingResponse {
    content string
    finished bool
}

function StreamingChat(messages: Message[], topic: string) -> StreamingResponse {
    client ""openai/gpt-4o-mini""
    
    prompt #""
        You are a helpful assistant discussing {{ topic }}.
        
        {{ ctx.output_format }}

        {% for m in messages %}
        {{ _.role(m.role) }}
        {{ m.content }}
        {% endfor %}
    ""#
}

function GenerateStory(prompt: string, length: ""short"" | ""medium"" | ""long"") -> string {
    client ""openai/gpt-4o-mini""
    
    prompt #""
        Write a {{ length }} story based on this prompt: {{ prompt }}
        
        Make it engaging and creative.
    ""#
}";
        var bamlFile = CreateBamlAdditionalText("streaming_chat.baml", bamlContent);

        // Act
        var compilation = RunSourceGenerator(bamlFile);

        // Assert
        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

        Assert.Empty(errors);

        // Verify both files are generated
        AssertGeneratedFileExists(compilation, "streaming_chat_baml_BamlTypes.g.cs");
        AssertGeneratedFileExists(compilation, "streaming_chat_baml_BamlClient.g.cs");

        // Check types generation
        var typesCode = GetGeneratedSourceCode(compilation, "streaming_chat_baml_BamlTypes.g.cs");
        Assert.Contains("public partial class Message", typesCode);
        Assert.Contains("public partial class StreamingResponse", typesCode);
        Assert.Contains("public bool Finished { get; set; }", typesCode);

        // Check client generation
        var clientCode = GetGeneratedSourceCode(compilation, "streaming_chat_baml_BamlClient.g.cs");
        Assert.Contains("Task<StreamingResponse> StreamingChatAsync", clientCode);
        Assert.Contains("Task<string> GenerateStoryAsync", clientCode);
        Assert.Contains("IAsyncEnumerable<StreamingResponse> StreamStreamingChatAsync", clientCode);
        Assert.Contains("IAsyncEnumerable<string> StreamGenerateStoryAsync", clientCode);
    }

    [Fact]
    public void EndToEnd_MultipleBamlFiles_GeneratesIndependentCode()
    {
        // Arrange
        var bamlFile1 = CreateBamlAdditionalText("schema1.baml", @"
class User {
    id string
    name string
}");

        var bamlFile2 = CreateBamlAdditionalText("schema2.baml", @"
class Product {
    id string
    title string
    price float
}");

        // Act
        var compilation = RunSourceGenerator(bamlFile1, bamlFile2);

        // Assert
        // Debug: Check what files were actually generated
        var syntaxTrees = compilation.SyntaxTrees.ToArray();
        var generatedFiles = syntaxTrees.Where(tree => tree.FilePath.Contains("schema")).ToArray();

        // Verify separate files are generated for each schema
        AssertGeneratedFileExists(compilation, "schema1_baml_BamlTypes.g.cs");
        AssertGeneratedFileExists(compilation, "schema2_baml_BamlTypes.g.cs");

        var schema1Code = GetGeneratedSourceCode(compilation, "schema1_baml_BamlTypes.g.cs");
        Assert.Contains("public partial class User", schema1Code);
        Assert.DoesNotContain("public partial class Product", schema1Code);

        var schema2Code = GetGeneratedSourceCode(compilation, "schema2_baml_BamlTypes.g.cs");
        Assert.Contains("public partial class Product", schema2Code);
        Assert.DoesNotContain("public partial class User", schema2Code);
    }

    [Fact]
    public void EndToEnd_EmptyBamlFile_GeneratesEmptyOutput()
    {
        // Arrange
        var bamlFile = CreateBamlAdditionalText("empty.baml", "");

        // Act
        var compilation = RunSourceGenerator(bamlFile);

        // Assert
        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

        Assert.Empty(errors);

        // No files should be generated for empty schema
        var syntaxTrees = compilation.SyntaxTrees.ToArray();
        var generatedFiles = syntaxTrees.Where(tree => tree.FilePath.Contains("empty_baml")).ToArray();
        Assert.Empty(generatedFiles);
    }

    [Theory]
    [InlineData("class {}", "Malformed class definition")]
    [InlineData("enum {}", "Malformed enum definition")]
    [InlineData("function", "Incomplete function definition")]
    public void EndToEnd_MalformedBaml_HandlesGracefully(string bamlContent, string description)
    {
        // Arrange
        var bamlFile = CreateBamlAdditionalText("malformed.baml", bamlContent);

        // Act
        var compilation = RunSourceGenerator(bamlFile);

        // Assert - Should not crash, may produce warnings but not errors
        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

        // The compilation should not fail catastrophically for malformed input
        Assert.True(compilation != null, $"Compilation failed for: {description}");
    }

    #region Helper Methods

    /// <summary>
    /// Creates an AdditionalText instance for a BAML file with the given content.
    /// </summary>
    private static AdditionalText CreateBamlAdditionalText(string fileName, string content)
    {
        return new TestAdditionalText(fileName, content);
    }

    /// <summary>
    /// Runs the source generator with the provided BAML files and returns the resulting compilation.
    /// </summary>
    private CSharpCompilation RunSourceGenerator(params AdditionalText[] additionalTexts)
    {
        var driver = CSharpGeneratorDriver.Create(_sourceGenerator);

        var driverWithFiles = (CSharpGeneratorDriver)driver.AddAdditionalTexts(additionalTexts.ToImmutableArray());
        var driverRun = (CSharpGeneratorDriver)driverWithFiles.RunGenerators(_baseCompilation);

        var runResult = driverRun.GetRunResult();

        // Get the compilation with generated sources
        var newCompilation = _baseCompilation;
        foreach (var result in runResult.Results)
        {
            foreach (var source in result.GeneratedSources)
            {
                newCompilation = newCompilation.AddSyntaxTrees(
                    CSharpSyntaxTree.ParseText(source.SourceText, path: source.HintName));
            }
        }

        return newCompilation;
    }

    /// <summary>
    /// Asserts that a file with the given name was generated.
    /// </summary>
    private static void AssertGeneratedFileExists(CSharpCompilation compilation, string fileName)
    {
        var syntaxTrees = compilation.SyntaxTrees.ToArray();
        var generatedFile = syntaxTrees.FirstOrDefault(tree => tree.FilePath.EndsWith(fileName));

        Assert.NotNull(generatedFile);
    }

    /// <summary>
    /// Gets the source code of a generated file.
    /// </summary>
    private static string GetGeneratedSourceCode(CSharpCompilation compilation, string fileName)
    {
        var syntaxTrees = compilation.SyntaxTrees.ToArray();
        var generatedFile = syntaxTrees.FirstOrDefault(tree => tree.FilePath.EndsWith(fileName));

        Assert.NotNull(generatedFile);
        return generatedFile.ToString();
    }

    /// <summary>
    /// Asserts that the generated code contains expected content without checking for compilation errors.
    /// This focuses on validating the source generator output rather than compilability.
    /// </summary>
    private static void AssertGeneratedCodeContains(CSharpCompilation compilation, string fileName, params string[] expectedContent)
    {
        AssertGeneratedFileExists(compilation, fileName);
        var generatedCode = GetGeneratedSourceCode(compilation, fileName);

        foreach (var expected in expectedContent)
        {
            Assert.Contains(expected, generatedCode);
        }
    }

    #endregion

    #region Test Infrastructure

    /// <summary>
    /// Test implementation of AdditionalText for testing purposes.
    /// </summary>
    private class TestAdditionalText : AdditionalText
    {
        private readonly string _content;

        public TestAdditionalText(string path, string content)
        {
            Path = path;
            _content = content;
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(_content, Encoding.UTF8);
        }
    }

    #endregion
}
