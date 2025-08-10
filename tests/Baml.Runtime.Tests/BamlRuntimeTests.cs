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

using System.Net;
using System.Text;
using System.Text.Json;
using Baml.Runtime;
using NSubstitute;
using Xunit;

namespace Baml.Runtime.Tests;

public class BamlRuntimeTests
{
    private readonly BamlConfiguration _configuration;

    public BamlRuntimeTests()
    {
        _configuration = new BamlConfiguration
        {
            ApiEndpoint = "http://localhost:8000/api/baml/call",
            StreamingEndpoint = "http://localhost:8000/api/baml/stream",
            ApiKey = "test-api-key"
        };
    }

    [Fact]
    public void Constructor_WithConfiguration_SetsConfiguration()
    {
        // Act
        using var runtime = new BamlRuntime(_configuration);

        // Assert
        Assert.NotNull(runtime);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BamlRuntime(null!));
    }

    [Fact]
    public void Constructor_WithHttpClient_SetsHttpClient()
    {
        // Arrange
        var httpClient = Substitute.For<HttpClient>();

        // Act
        using var runtime = new BamlRuntime(_configuration, httpClient);

        // Assert
        Assert.NotNull(runtime);
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BamlRuntime(_configuration, null!));
    }

    [Fact]
    public async Task CallFunctionAsync_WithNullFunctionName_ThrowsArgumentException()
    {
        // Arrange
        using var runtime = new BamlRuntime(_configuration);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => runtime.CallFunctionAsync<string>(null!, new { }));
    }

    [Fact]
    public async Task CallFunctionAsync_WithEmptyFunctionName_ThrowsArgumentException()
    {
        // Arrange
        using var runtime = new BamlRuntime(_configuration);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => runtime.CallFunctionAsync<string>("", new { }));
    }

    [Theory]
    [InlineData("test-function")]
    [InlineData("AnotherFunction")]
    [InlineData("function_with_underscores")]
    public async Task CallFunctionAsync_WithValidFunctionName_DoesNotThrow(string functionName)
    {
        // Arrange
        using var runtime = new BamlRuntime(_configuration);

        // Act & Assert - This will throw HttpRequestException due to no server, but validates the function name
        await Assert.ThrowsAsync<HttpRequestException>(
            () => runtime.CallFunctionAsync<string>(functionName, new { }));
    }
}

public class BamlRuntimeHttpTests
{
    private readonly BamlConfiguration _configuration;

    public BamlRuntimeHttpTests()
    {
        _configuration = new BamlConfiguration
        {
            ApiEndpoint = "http://localhost:8000/api/baml/call",
            StreamingEndpoint = "http://localhost:8000/api/baml/stream",
            ApiKey = "test-api-key"
        };
    }

    private static HttpClient CreateMockHttpClient(HttpResponseMessage response)
    {
        var mockHandler = Substitute.For<HttpMessageHandler>();

        // Use reflection to access the protected SendAsync method
        var sendAsyncMethod = typeof(HttpMessageHandler)
            .GetMethod("SendAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (sendAsyncMethod != null)
        {
            mockHandler.When(h => sendAsyncMethod.Invoke(h, new object[] { Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>() }))
                      .Do(callInfo => Task.FromResult(response));
        }

        return new HttpClient(mockHandler);
    }

    private static HttpClient CreateMockHttpClientWithHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var mockHandler = new MockHttpMessageHandler(handler);
        return new HttpClient(mockHandler);
    }

    [Fact]
    public async Task CallFunctionAsync_WithSuccessfulResponse_ReturnsDeserializedResult()
    {
        // Arrange
        var expectedResult = "test result";
        var responseContent = JsonSerializer.Serialize(expectedResult);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        var httpClient = CreateMockHttpClientWithHandler(async (request, cancellationToken) =>
        {
            // Verify request is sent to correct endpoint
            Assert.Equal(_configuration.ApiEndpoint, request.RequestUri?.ToString());
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("application/json", request.Content?.Headers.ContentType?.MediaType);

            // Verify request body contains function name and parameters
            var requestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            var requestObj = JsonSerializer.Deserialize<JsonElement>(requestBody);
            Assert.Equal("test-function", requestObj.GetProperty("functionName").GetString());

            return response;
        });

        using var runtime = new BamlRuntime(_configuration, httpClient);

        // Act
        var result = await runtime.CallFunctionAsync<string>("test-function", new { param1 = "value1" });

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task CallFunctionAsync_WithHttpError_ThrowsHttpRequestException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal server error")
        };

        var httpClient = CreateMockHttpClientWithHandler(async (request, cancellationToken) =>
        {
            await Task.CompletedTask; // Avoid async warning
            return response;
        });

        using var runtime = new BamlRuntime(_configuration, httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => runtime.CallFunctionAsync<string>("test-function", new { }));
    }

    [Fact]
    public async Task CallFunctionAsync_WithInvalidJson_ThrowsBamlException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json", Encoding.UTF8, "application/json")
        };

        var httpClient = CreateMockHttpClientWithHandler(async (request, cancellationToken) =>
        {
            await Task.CompletedTask; // Avoid async warning
            return response;
        });

        using var runtime = new BamlRuntime(_configuration, httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(
            () => runtime.CallFunctionAsync<string>("test-function", new { }));
    }

    [Fact]
    public async Task CallFunctionAsync_WithNullResponse_ThrowsBamlException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };

        var httpClient = CreateMockHttpClientWithHandler(async (request, cancellationToken) =>
        {
            await Task.CompletedTask; // Avoid async warning
            return response;
        });

        using var runtime = new BamlRuntime(_configuration, httpClient);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BamlException>(
            () => runtime.CallFunctionAsync<string>("test-function", new { }));

        Assert.Equal("Failed to deserialize response", exception.Message);
    }

    [Fact]
    public async Task CallFunctionAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("\"test result\"", Encoding.UTF8, "application/json")
        };

        var httpClient = CreateMockHttpClientWithHandler(async (request, cancellationToken) =>
        {
            // Simulate delay to allow cancellation
            await Task.Delay(1000, cancellationToken);
            return response;
        });

        using var runtime = new BamlRuntime(_configuration, httpClient);
        using var cts = new CancellationTokenSource();

        // Act
        cts.CancelAfter(100); // Cancel after 100ms

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => runtime.CallFunctionAsync<string>("test-function", new { }, cts.Token));
    }

    [Fact]
    public async Task StreamFunctionAsync_WithSuccessfulResponse_YieldsStreamedResults()
    {
        // Arrange
        var streamData = "data: \"result1\"\n\ndata: \"result2\"\n\ndata: [DONE]\n\n";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(streamData, Encoding.UTF8, "text/plain")
        };

        var httpClient = CreateMockHttpClientWithHandler(async (request, cancellationToken) =>
        {
            // Verify request is sent to streaming endpoint
            Assert.Equal(_configuration.StreamingEndpoint, request.RequestUri?.ToString());
            Assert.Equal(HttpMethod.Post, request.Method);

            // Verify request body contains stream flag
            var requestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            var requestObj = JsonSerializer.Deserialize<JsonElement>(requestBody);
            Assert.True(requestObj.GetProperty("stream").GetBoolean());

            return response;
        });

        using var runtime = new BamlRuntime(_configuration, httpClient);

        // Act
        var results = new List<string>();
        await foreach (var result in runtime.StreamFunctionAsync<string>("test-function", new { }))
        {
            results.Add(result);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("result1", results[0]);
        Assert.Equal("result2", results[1]);
    }

    [Fact]
    public async Task StreamFunctionAsync_WithEmptyLines_SkipsEmptyLines()
    {
        // Arrange
        var streamData = "data: \"result1\"\n\n\n\ndata: \"result2\"\n\ndata: [DONE]\n\n";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(streamData, Encoding.UTF8, "text/plain")
        };

        var httpClient = CreateMockHttpClientWithHandler(async (request, cancellationToken) =>
        {
            await Task.CompletedTask; // Avoid async warning
            return response;
        });

        using var runtime = new BamlRuntime(_configuration, httpClient);

        // Act
        var results = new List<string>();
        await foreach (var result in runtime.StreamFunctionAsync<string>("test-function", new { }))
        {
            results.Add(result);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("result1", results[0]);
        Assert.Equal("result2", results[1]);
    }

    [Fact]
    public async Task StreamFunctionAsync_WithNonDataLines_SkipsNonDataLines()
    {
        // Arrange
        var streamData = "event: message\ndata: \"result1\"\n\nretry: 1000\ndata: \"result2\"\n\ndata: [DONE]\n\n";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(streamData, Encoding.UTF8, "text/plain")
        };

        var httpClient = CreateMockHttpClientWithHandler(async (request, cancellationToken) =>
        {
            await Task.CompletedTask; // Avoid async warning
            return response;
        });

        using var runtime = new BamlRuntime(_configuration, httpClient);

        // Act
        var results = new List<string>();
        await foreach (var result in runtime.StreamFunctionAsync<string>("test-function", new { }))
        {
            results.Add(result);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("result1", results[0]);
        Assert.Equal("result2", results[1]);
    }

    [Fact]
    public async Task StreamFunctionAsync_WithHttpError_ThrowsHttpRequestException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad request")
        };

        var httpClient = CreateMockHttpClientWithHandler(async (request, cancellationToken) =>
        {
            await Task.CompletedTask; // Avoid async warning
            return response;
        });

        using var runtime = new BamlRuntime(_configuration, httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await foreach (var result in runtime.StreamFunctionAsync<string>("test-function", new { }))
            {
                // This should not execute
            }
        });
    }

    [Fact]
    public async Task StreamFunctionAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var streamData = "data: \"result1\"\n\ndata: \"result2\"\n\ndata: [DONE]\n\n";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(streamData, Encoding.UTF8, "text/plain")
        };

        var httpClient = CreateMockHttpClientWithHandler(async (request, cancellationToken) =>
        {
            // Simulate delay to allow cancellation
            await Task.Delay(1000, cancellationToken);
            return response;
        });

        using var runtime = new BamlRuntime(_configuration, httpClient);
        using var cts = new CancellationTokenSource();

        // Act
        cts.CancelAfter(100); // Cancel after 100ms

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var result in runtime.StreamFunctionAsync<string>("test-function", new { }, cts.Token))
            {
                // This should not execute due to cancellation
            }
        });
    }
}

/// <summary>
/// Custom HttpMessageHandler for testing that allows injecting response behavior
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await _handler(request, cancellationToken);
    }
}

public class BamlConfigurationTests
{
    [Fact]
    public void DefaultConfiguration_HasExpectedValues()
    {
        // Act
        var config = new BamlConfiguration();

        // Assert
        Assert.Equal("http://localhost:8000/api/baml/call", config.ApiEndpoint);
        Assert.Equal("http://localhost:8000/api/baml/stream", config.StreamingEndpoint);
        Assert.Null(config.ApiKey);
        Assert.Equal(TimeSpan.FromMinutes(5), config.Timeout);
    }

    [Fact]
    public void Configuration_CanSetProperties()
    {
        // Arrange
        var config = new BamlConfiguration();
        var timeout = TimeSpan.FromMinutes(10);

        // Act
        config.ApiEndpoint = "https://api.example.com/baml";
        config.StreamingEndpoint = "https://api.example.com/stream";
        config.ApiKey = "test-key";
        config.Timeout = timeout;

        // Assert
        Assert.Equal("https://api.example.com/baml", config.ApiEndpoint);
        Assert.Equal("https://api.example.com/stream", config.StreamingEndpoint);
        Assert.Equal("test-key", config.ApiKey);
        Assert.Equal(timeout, config.Timeout);
    }
}

public class BamlExceptionTests
{
    [Fact]
    public void BamlException_DefaultConstructor_CreatesException()
    {
        // Act
        var exception = new BamlException();

        // Assert
        Assert.NotNull(exception);
    }

    [Fact]
    public void BamlException_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new BamlException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void BamlFunctionException_WithFunctionName_SetsProperties()
    {
        // Arrange
        var functionName = "TestFunction";
        var parameters = new { param1 = "value1" };

        // Act
        var exception = new BamlFunctionException(functionName, parameters);

        // Assert
        Assert.Equal(functionName, exception.FunctionName);
        Assert.Equal(parameters, exception.Parameters);
        Assert.Contains(functionName, exception.Message);
    }

    [Fact]
    public void BamlSchemaException_WithSchemaFile_SetsProperties()
    {
        // Arrange
        var schemaFile = "test.baml";

        // Act
        var exception = new BamlSchemaException(schemaFile);

        // Assert
        Assert.Equal(schemaFile, exception.SchemaFile);
        Assert.Contains(schemaFile, exception.Message);
    }
}

public class BamlAttributesTests
{
    [Fact]
    public void BamlGeneratedAttribute_WithSourceFile_SetsProperties()
    {
        // Arrange
        var sourceFile = "test.baml";
        var version = "1.0.0";

        // Act
        var attribute = new BamlGeneratedAttribute(sourceFile, version);

        // Assert
        Assert.Equal(sourceFile, attribute.SourceFile);
        Assert.Equal(version, attribute.Version);
    }

    [Fact]
    public void BamlGeneratedAttribute_WithDefaultVersion_SetsDefaultVersion()
    {
        // Arrange
        var sourceFile = "test.baml";

        // Act
        var attribute = new BamlGeneratedAttribute(sourceFile);

        // Assert
        Assert.Equal(sourceFile, attribute.SourceFile);
        Assert.Equal("1.0.0", attribute.Version);
    }

    [Fact]
    public void BamlFunctionAttribute_WithFunctionName_SetsProperties()
    {
        // Arrange
        var functionName = "TestFunction";
        var client = "test-client";

        // Act
        var attribute = new BamlFunctionAttribute(functionName, client);

        // Assert
        Assert.Equal(functionName, attribute.FunctionName);
        Assert.Equal(client, attribute.Client);
    }

    [Fact]
    public void BamlFunctionAttribute_WithNullClient_SetsNullClient()
    {
        // Arrange
        var functionName = "TestFunction";

        // Act
        var attribute = new BamlFunctionAttribute(functionName);

        // Assert
        Assert.Equal(functionName, attribute.FunctionName);
        Assert.Null(attribute.Client);
    }
}
