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
using Xunit;
using System.Linq;

namespace Baml.SourceGenerator.Tests;

public class BamlParserTests
{
    private readonly BamlParser _parser;

    public BamlParserTests()
    {
        _parser = new BamlParser();
    }

    [Fact]
    public void Parse_EmptyContent_ReturnsEmptySchema()
    {
        // Arrange
        var content = "";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.NotNull(schema);
        Assert.Equal(filePath, schema.FilePath);
        Assert.Empty(schema.Classes);
        Assert.Empty(schema.Enums);
        Assert.Empty(schema.Functions);
    }

    [Fact]
    public void Parse_SimpleClass_ParsesCorrectly()
    {
        // Arrange
        var content = @"
class Message {
    role string
    content string
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Classes);
        var messageClass = schema.Classes[0];
        Assert.Equal("Message", messageClass.Name);
        Assert.Equal(2, messageClass.Properties.Count);

        var roleProperty = messageClass.Properties.First(p => p.Name == "role");
        Assert.Equal("string", roleProperty.Type);

        var contentProperty = messageClass.Properties.First(p => p.Name == "content");
        Assert.Equal("string", contentProperty.Type);
    }

    [Fact]
    public void Parse_SimpleEnum_ParsesCorrectly()
    {
        // Arrange
        var content = @"
enum Tone {
    ""happy"",
    ""sad"",
    ""neutral""
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Enums);
        var toneEnum = schema.Enums[0];
        Assert.Equal("Tone", toneEnum.Name);
        Assert.Equal(3, toneEnum.Values.Count);
        Assert.Contains("happy", toneEnum.Values);
        Assert.Contains("sad", toneEnum.Values);
        Assert.Contains("neutral", toneEnum.Values);
    }

    [Fact]
    public void Parse_SimpleFunction_ParsesCorrectly()
    {
        // Arrange
        var content = @"
function ChatAgent(messages: Message[], tone: string) -> ReplyTool {
    client ""openai/gpt-4o-mini""
    
    prompt #""
        Be a {{ tone }} bot.
    ""#
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Functions);
        var chatFunction = schema.Functions[0];
        Assert.Equal("ChatAgent", chatFunction.Name);
        Assert.Equal("ReplyTool", chatFunction.ReturnType);
        Assert.Equal("openai/gpt-4o-mini", chatFunction.Client);
        Assert.Equal(2, chatFunction.Parameters.Count);

        var messagesParam = chatFunction.Parameters.First(p => p.Name == "messages");
        Assert.Equal("Message[]", messagesParam.Type);

        var toneParam = chatFunction.Parameters.First(p => p.Name == "tone");
        Assert.Equal("string", toneParam.Type);
    }

    [Fact]
    public void Parse_ComplexSchema_ParsesAllElements()
    {
        // Arrange
        var content = @"
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
        var filePath = "complex.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Equal(filePath, schema.FilePath);
        Assert.Equal(2, schema.Classes.Count);
        Assert.Single(schema.Enums);
        Assert.Single(schema.Functions);

        // Verify class names
        Assert.Contains(schema.Classes, c => c.Name == "Message");
        Assert.Contains(schema.Classes, c => c.Name == "ReplyTool");

        // Verify enum
        Assert.Equal("Tone", schema.Enums[0].Name);

        // Verify function
        Assert.Equal("ChatAgent", schema.Functions[0].Name);
    }

    [Theory]
    [InlineData("class Test {}", 1, 0, 0)]
    [InlineData("enum Test { \"a\", \"b\" }", 0, 1, 0)]
    [InlineData("function Test() -> string { client \"test\" }", 0, 0, 1)]
    public void Parse_DifferentElementTypes_ParsesCorrectCount(string content, int expectedClasses, int expectedEnums, int expectedFunctions)
    {
        // Act
        var schema = _parser.Parse(content, "test.baml");

        // Assert
        Assert.Equal(expectedClasses, schema.Classes.Count);
        Assert.Equal(expectedEnums, schema.Enums.Count);
        Assert.Equal(expectedFunctions, schema.Functions.Count);
    }

    // Tests for literal values in properties (Task 9.1)
    [Fact]
    public void Parse_ClassWithLiteralValue_ParsesCorrectly()
    {
        // Arrange
        var content = @"
class Action {
    type ""stop""
    message string
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Classes);
        var actionClass = schema.Classes[0];
        Assert.Equal("Action", actionClass.Name);
        Assert.Equal(2, actionClass.Properties.Count);

        var typeProperty = actionClass.Properties.First(p => p.Name == "type");
        Assert.Equal("string", typeProperty.Type); // literal values default to string type
        Assert.Equal("stop", typeProperty.LiteralValue);

        var messageProperty = actionClass.Properties.First(p => p.Name == "message");
        Assert.Equal("string", messageProperty.Type);
        Assert.Null(messageProperty.LiteralValue);
    }

    [Fact]
    public void Parse_ClassWithMultipleLiteralValues_ParsesCorrectly()
    {
        // Arrange
        var content = @"
class Constants {
    status ""active""
    version ""1.0""
    enabled ""true""
    count int
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Classes);
        var constantsClass = schema.Classes[0];
        Assert.Equal("Constants", constantsClass.Name);
        Assert.Equal(4, constantsClass.Properties.Count);

        var statusProperty = constantsClass.Properties.First(p => p.Name == "status");
        Assert.Equal("string", statusProperty.Type);
        Assert.Equal("active", statusProperty.LiteralValue);

        var versionProperty = constantsClass.Properties.First(p => p.Name == "version");
        Assert.Equal("string", versionProperty.Type);
        Assert.Equal("1.0", versionProperty.LiteralValue);

        var enabledProperty = constantsClass.Properties.First(p => p.Name == "enabled");
        Assert.Equal("string", enabledProperty.Type);
        Assert.Equal("true", enabledProperty.LiteralValue);

        var countProperty = constantsClass.Properties.First(p => p.Name == "count");
        Assert.Equal("int", countProperty.Type);
        Assert.Null(countProperty.LiteralValue);
    }

    [Theory]
    [InlineData("action \"stop\"", "action", "string", "stop")]
    [InlineData("value \"123\"", "value", "string", "123")]
    [InlineData("flag \"true\"", "flag", "string", "true")]
    [InlineData("path \"/api/v1\"", "path", "string", "/api/v1")]
    [InlineData("message \"Hello, World!\"", "message", "string", "Hello, World!")]
    public void Parse_PropertyWithLiteralValue_ParsesCorrectly(string propertyLine, string expectedName, string expectedType, string expectedLiteral)
    {
        // Arrange
        var content = $@"
class Test {{
    {propertyLine}
}}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Classes);
        var testClass = schema.Classes[0];
        Assert.Single(testClass.Properties);

        var property = testClass.Properties[0];
        Assert.Equal(expectedName, property.Name);
        Assert.Equal(expectedType, property.Type);
        Assert.Equal(expectedLiteral, property.LiteralValue);
    }

    [Fact]
    public void Parse_MixedPropertiesWithAndWithoutLiterals_ParsesCorrectly()
    {
        // Arrange
        var content = @"
class MixedProperties {
    id string
    action ""create""
    name string
    type ""user""
    active bool
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Classes);
        var mixedClass = schema.Classes[0];
        Assert.Equal(5, mixedClass.Properties.Count);

        // Regular properties
        var idProperty = mixedClass.Properties.First(p => p.Name == "id");
        Assert.Equal("string", idProperty.Type);
        Assert.Null(idProperty.LiteralValue);

        var nameProperty = mixedClass.Properties.First(p => p.Name == "name");
        Assert.Equal("string", nameProperty.Type);
        Assert.Null(nameProperty.LiteralValue);

        var activeProperty = mixedClass.Properties.First(p => p.Name == "active");
        Assert.Equal("bool", activeProperty.Type);
        Assert.Null(activeProperty.LiteralValue);

        // Literal properties
        var actionProperty = mixedClass.Properties.First(p => p.Name == "action");
        Assert.Equal("string", actionProperty.Type);
        Assert.Equal("create", actionProperty.LiteralValue);

        var typeProperty = mixedClass.Properties.First(p => p.Name == "type");
        Assert.Equal("string", typeProperty.Type);
        Assert.Equal("user", typeProperty.LiteralValue);
    }

    // Tests for property descriptions (@description) (Task 9.2)
    [Fact]
    public void Parse_PropertyWithDescription_ParsesCorrectly()
    {
        // Arrange
        var content = @"
class User {
    name string @description(""User's full name"")
    email string @description(""User's email address"")
    age int
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Classes);
        var userClass = schema.Classes[0];
        Assert.Equal("User", userClass.Name);
        Assert.Equal(3, userClass.Properties.Count);

        var nameProperty = userClass.Properties.First(p => p.Name == "name");
        Assert.Equal("string", nameProperty.Type);
        Assert.Equal("User's full name", nameProperty.Description);
        Assert.Null(nameProperty.LiteralValue);

        var emailProperty = userClass.Properties.First(p => p.Name == "email");
        Assert.Equal("string", emailProperty.Type);
        Assert.Equal("User's email address", emailProperty.Description);
        Assert.Null(emailProperty.LiteralValue);

        var ageProperty = userClass.Properties.First(p => p.Name == "age");
        Assert.Equal("int", ageProperty.Type);
        Assert.Null(ageProperty.Description);
        Assert.Null(ageProperty.LiteralValue);
    }

    [Theory]
    [InlineData("name string @description(\"Full name\")", "name", "string", "Full name", "")]
    [InlineData("id int @description(\"Unique identifier\")", "id", "int", "Unique identifier", "")]
    [InlineData("status \"active\" @description(\"Current status\")", "status", "string", "Current status", "active")]
    [InlineData("enabled bool @description(\"Whether feature is enabled\")", "enabled", "bool", "Whether feature is enabled", "")]
    public void Parse_PropertyWithDescriptionVariations_ParsesCorrectly(string propertyLine, string expectedName, string expectedType, string expectedDescription, string? expectedLiteral)
    {
        // Arrange
        var content = $@"
class Test {{
    {propertyLine}
}}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Classes);
        var testClass = schema.Classes[0];
        Assert.Single(testClass.Properties);

        var property = testClass.Properties[0];
        Assert.Equal(expectedName, property.Name);
        Assert.Equal(expectedType, property.Type);
        Assert.Equal(expectedDescription, property.Description);

        if (string.IsNullOrEmpty(expectedLiteral))
        {
            Assert.Null(property.LiteralValue);
        }
        else
        {
            Assert.Equal(expectedLiteral, property.LiteralValue);
        }
    }

    [Fact]
    public void Parse_LiteralValueWithDescription_ParsesCorrectly()
    {
        // Arrange
        var content = @"
class Config {
    version ""1.0"" @description(""API version"")
    environment ""production"" @description(""Deployment environment"")
    debug bool @description(""Enable debug mode"")
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Classes);
        var configClass = schema.Classes[0];
        Assert.Equal("Config", configClass.Name);
        Assert.Equal(3, configClass.Properties.Count);

        var versionProperty = configClass.Properties.First(p => p.Name == "version");
        Assert.Equal("string", versionProperty.Type);
        Assert.Equal("1.0", versionProperty.LiteralValue);
        Assert.Equal("API version", versionProperty.Description);

        var environmentProperty = configClass.Properties.First(p => p.Name == "environment");
        Assert.Equal("string", environmentProperty.Type);
        Assert.Equal("production", environmentProperty.LiteralValue);
        Assert.Equal("Deployment environment", environmentProperty.Description);

        var debugProperty = configClass.Properties.First(p => p.Name == "debug");
        Assert.Equal("bool", debugProperty.Type);
        Assert.Null(debugProperty.LiteralValue);
        Assert.Equal("Enable debug mode", debugProperty.Description);
    }

    [Fact]
    public void Parse_PropertyWithoutDescription_HasNullDescription()
    {
        // Arrange
        var content = @"
class Simple {
    id string
    name string
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Classes);
        var simpleClass = schema.Classes[0];
        Assert.Equal(2, simpleClass.Properties.Count);

        foreach (var property in simpleClass.Properties)
        {
            Assert.Null(property.Description);
        }
    }

    [Fact]
    public void Parse_MixedPropertiesWithAndWithoutDescriptions_ParsesCorrectly()
    {
        // Arrange
        var content = @"
class MixedDescriptions {
    id string
    name string @description(""User name"")
    email string
    status ""active"" @description(""User status"")
    created_at string @description(""Creation timestamp"")
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Classes);
        var mixedClass = schema.Classes[0];
        Assert.Equal(5, mixedClass.Properties.Count);

        // Properties without descriptions
        var idProperty = mixedClass.Properties.First(p => p.Name == "id");
        Assert.Null(idProperty.Description);

        var emailProperty = mixedClass.Properties.First(p => p.Name == "email");
        Assert.Null(emailProperty.Description);

        // Properties with descriptions
        var nameProperty = mixedClass.Properties.First(p => p.Name == "name");
        Assert.Equal("User name", nameProperty.Description);

        var statusProperty = mixedClass.Properties.First(p => p.Name == "status");
        Assert.Equal("User status", statusProperty.Description);
        Assert.Equal("active", statusProperty.LiteralValue);

        var createdAtProperty = mixedClass.Properties.First(p => p.Name == "created_at");
        Assert.Equal("Creation timestamp", createdAtProperty.Description);
    }

    // Tests for edge cases and error handling (Task 9.3)
    [Fact]
    public void Parse_EmptyClass_ParsesCorrectly()
    {
        // Arrange
        var content = @"
class EmptyClass {
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Classes);
        var emptyClass = schema.Classes[0];
        Assert.Equal("EmptyClass", emptyClass.Name);
        Assert.Empty(emptyClass.Properties);
    }

    [Fact]
    public void Parse_EmptyEnum_ParsesCorrectly()
    {
        // Arrange
        var content = @"
enum EmptyEnum {
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Enums);
        var emptyEnum = schema.Enums[0];
        Assert.Equal("EmptyEnum", emptyEnum.Name);
        Assert.Empty(emptyEnum.Values);
    }

    [Fact]
    public void Parse_FunctionWithoutParameters_ParsesCorrectly()
    {
        // Arrange
        var content = @"
function SimpleFunction() -> string {
    client ""test-client""
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Functions);
        var simpleFunction = schema.Functions[0];
        Assert.Equal("SimpleFunction", simpleFunction.Name);
        Assert.Equal("string", simpleFunction.ReturnType);
        Assert.Equal("test-client", simpleFunction.Client);
        Assert.Empty(simpleFunction.Parameters);
    }

    [Fact]
    public void Parse_FunctionWithoutClient_ParsesCorrectly()
    {
        // Arrange
        var content = @"
function NoClientFunction(input: string) -> string {
    prompt #""
        Process {{ input }}
    ""#
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Functions);
        var noClientFunction = schema.Functions[0];
        Assert.Equal("NoClientFunction", noClientFunction.Name);
        Assert.Equal("string", noClientFunction.ReturnType);
        Assert.Null(noClientFunction.Client);
        Assert.Single(noClientFunction.Parameters);
    }

    [Theory]
    [InlineData("class {}", 0)] // No name
    [InlineData("class Test", 0)] // No braces  
    [InlineData("class Test { invalid_property_format }", 1)] // Invalid property
    [InlineData("enum {}", 0)] // No name
    [InlineData("enum Test", 0)] // No braces
    public void Parse_MalformedInputs_HandlesGracefully(string content, int expectedElementCount)
    {
        // Arrange
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.NotNull(schema);
        var totalElements = schema.Classes.Count + schema.Enums.Count + schema.Functions.Count;
        Assert.Equal(expectedElementCount, totalElements);
    }

    [Fact]
    public void Parse_MultiLinePropertyFormats_ParsesCorrectly()
    {
        // Arrange
        var content = @"
class MultiLineTest {
    prop1    string
    prop2	string
      prop3 string  
	prop4 ""literal""    @description(""Test"")   
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Classes);
        var testClass = schema.Classes[0];
        Assert.Equal(4, testClass.Properties.Count);

        // Verify all properties parsed despite irregular spacing
        var propNames = testClass.Properties.Select(p => p.Name).ToList();
        Assert.Contains("prop1", propNames);
        Assert.Contains("prop2", propNames);
        Assert.Contains("prop3", propNames);
        Assert.Contains("prop4", propNames);

        var prop4 = testClass.Properties.First(p => p.Name == "prop4");
        Assert.Equal("literal", prop4.LiteralValue);
        Assert.Equal("Test", prop4.Description);
    }

    [Fact]
    public void Parse_SpecialCharactersInStrings_ParsesCorrectly()
    {
        // Arrange
        var content = @"
class SpecialChars {
    message ""Hello, World! 🌍""
    path ""/api/v1/users/123""
    unicode ""测试""
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Classes);
        var specialClass = schema.Classes[0];
        Assert.Equal(3, specialClass.Properties.Count);

        var messageProperty = specialClass.Properties.First(p => p.Name == "message");
        Assert.Equal("Hello, World! 🌍", messageProperty.LiteralValue);

        var pathProperty = specialClass.Properties.First(p => p.Name == "path");
        Assert.Equal("/api/v1/users/123", pathProperty.LiteralValue);

        var unicodeProperty = specialClass.Properties.First(p => p.Name == "unicode");
        Assert.Equal("测试", unicodeProperty.LiteralValue);
    }

    [Fact]
    public void Parse_EnumWithVariousFormats_ParsesCorrectly()
    {
        // Arrange
        var content = @"
enum MixedEnum {
    ""quoted_value"",
    unquoted_value,
    ""another-quoted"",
    third_unquoted
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Enums);
        var mixedEnum = schema.Enums[0];
        Assert.Equal("MixedEnum", mixedEnum.Name);
        Assert.Equal(4, mixedEnum.Values.Count);

        Assert.Contains("quoted_value", mixedEnum.Values);
        Assert.Contains("unquoted_value", mixedEnum.Values);
        Assert.Contains("another-quoted", mixedEnum.Values);
        Assert.Contains("third_unquoted", mixedEnum.Values);
    }

    [Fact]
    public void Parse_ComplexFunctionParameters_ParsesCorrectly()
    {
        // Arrange
        var content = @"
function ComplexFunction(simple: string, array: string[], optional: bool) -> ResponseType {
    client ""complex-client""
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Functions);
        var complexFunction = schema.Functions[0];
        Assert.Equal("ComplexFunction", complexFunction.Name);
        Assert.Equal("ResponseType", complexFunction.ReturnType);
        Assert.Equal("complex-client", complexFunction.Client);
        Assert.Equal(3, complexFunction.Parameters.Count);

        var simpleParam = complexFunction.Parameters.First(p => p.Name == "simple");
        Assert.Equal("string", simpleParam.Type);

        var arrayParam = complexFunction.Parameters.First(p => p.Name == "array");
        Assert.Equal("string[]", arrayParam.Type);

        var optionalParam = complexFunction.Parameters.First(p => p.Name == "optional");
        Assert.Equal("bool", optionalParam.Type);
    }

    [Fact]
    public void Parse_DescriptionsWithSpecialCharacters_ParsesCorrectly()
    {
        // Arrange
        var content = @"
class SpecialDescriptions {
    name string @description(""User's full name"")
    email string @description(""Email address required"")
    path string @description(""File system path"")
}";
        var filePath = "test.baml";

        // Act
        var schema = _parser.Parse(content, filePath);

        // Assert
        Assert.Single(schema.Classes);
        var specialClass = schema.Classes[0];
        Assert.Equal(3, specialClass.Properties.Count);

        var nameProperty = specialClass.Properties.First(p => p.Name == "name");
        Assert.Equal("User's full name", nameProperty.Description);

        var emailProperty = specialClass.Properties.First(p => p.Name == "email");
        Assert.Equal("Email address required", emailProperty.Description);

        var pathProperty = specialClass.Properties.First(p => p.Name == "path");
        Assert.Equal("File system path", pathProperty.Description);
    }
}

public class BamlCodeGeneratorTests
{
    [Fact]
    public void Constructor_WithCustomNamespace_SetsNamespace()
    {
        // Arrange
        var customNamespace = "MyProject.Generated";

        // Act
        var generator = new BamlCodeGenerator(customNamespace);

        // Assert
        Assert.NotNull(generator);
    }

    [Fact]
    public void Constructor_WithDefaultNamespace_UsesDefault()
    {
        // Act
        var generator = new BamlCodeGenerator();

        // Assert
        Assert.NotNull(generator);
    }

    [Fact]
    public void Generate_EmptySchemas_ReturnsEmptyResult()
    {
        // Arrange
        var generator = new BamlCodeGenerator();
        var schemas = new List<BamlSchema>();

        // Act
        var result = generator.Generate(schemas);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Generate_WithSchemas_ReturnsGeneratedCode()
    {
        // Arrange
        var generator = new BamlCodeGenerator("Test.Generated");
        var schema = new BamlSchema
        {
            FilePath = "test.baml",
            Classes = new List<BamlClass>
            {
                new BamlClass
                {
                    Name = "TestClass",
                    Properties = new List<BamlProperty>
                    {
                        new BamlProperty { Name = "id", Type = "string" }
                    }
                }
            },
            Functions = new List<BamlFunction>
            {
                new BamlFunction
                {
                    Name = "TestFunction",
                    ReturnType = "TestClass",
                    Client = "test-client",
                    Parameters = new List<BamlParameter>()
                }
            }
        };

        // Act
        var result = generator.Generate(new List<BamlSchema> { schema });

        // Assert
        Assert.NotNull(result);
        Assert.Contains("BamlTypes.g.cs", result.Keys);
        Assert.Contains("BamlClient.g.cs", result.Keys);

        var typesCode = result["BamlTypes.g.cs"];
        Assert.Contains("namespace Test.Generated", typesCode);
        Assert.Contains("class TestClass", typesCode);

        var clientCode = result["BamlClient.g.cs"];
        Assert.Contains("namespace Test.Generated", clientCode);
        Assert.Contains("TestFunctionAsync", clientCode);
    }
}
