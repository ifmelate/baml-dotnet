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

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Baml.Runtime
{
    /// <summary>
    /// Default implementation of the BAML runtime.
    /// </summary>
    public class BamlRuntime : IBamlRuntime, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly BamlConfiguration _configuration;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed;

        public BamlRuntime(BamlConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpClient = new HttpClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public BamlRuntime(BamlConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<TResult> CallFunctionAsync<TResult>(string functionName, object parameters, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(functionName))
                throw new ArgumentException("Function name cannot be null or empty.", nameof(functionName));

            var request = new BamlRequest
            {
                FunctionName = functionName,
                Parameters = parameters
            };

            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_configuration.ApiEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<TResult>(responseJson, _jsonOptions);

            return result ?? throw new BamlException("Failed to deserialize response");
        }

        public async IAsyncEnumerable<TResult> StreamFunctionAsync<TResult>(string functionName, object parameters, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(functionName))
                throw new ArgumentException("Function name cannot be null or empty.", nameof(functionName));

            var request = new BamlRequest
            {
                FunctionName = functionName,
                Parameters = parameters,
                Stream = true
            };

            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_configuration.StreamingEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new System.IO.StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Handle Server-Sent Events format
                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    if (data == "[DONE]")
                        break;

                    var partialResult = JsonSerializer.Deserialize<TResult>(data, _jsonOptions);
                    if (partialResult != null)
                        yield return partialResult;
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Configuration for the BAML runtime.
    /// </summary>
    public class BamlConfiguration
    {
        public string ApiEndpoint { get; set; } = "http://localhost:8000/api/baml/call";
        public string StreamingEndpoint { get; set; } = "http://localhost:8000/api/baml/stream";
        public string? ApiKey { get; set; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Request model for BAML function calls.
    /// </summary>
    internal class BamlRequest
    {
        public string FunctionName { get; set; } = string.Empty;
        public object? Parameters { get; set; }
        public bool Stream { get; set; }
    }
}

