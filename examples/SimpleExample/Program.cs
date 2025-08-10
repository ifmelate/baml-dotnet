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

﻿using Baml.Runtime;
using Baml.Generated;

namespace SimpleExample;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("BAML .NET Client Example");
        Console.WriteLine("========================");

        // Configure the BAML runtime
        var configuration = new BamlConfiguration
        {
            ApiEndpoint = "http://localhost:8000/api/baml/call",
            StreamingEndpoint = "http://localhost:8000/api/baml/stream",
            ApiKey = Environment.GetEnvironmentVariable("BAML_API_KEY") ?? "your-api-key-here"
        };

        // Create the runtime and client
        using var runtime = new BamlRuntime(configuration);
        var client = new BamlGeneratedClient(runtime);

        // Example 1: Simple text extraction
        Console.WriteLine("\n--- Example 1: Text Extraction ---");
        try
        {
            var extractResult = await client.ExtractInfoAsync(
                "John Doe is a software engineer at Microsoft. He lives in Seattle and has 5 years of experience.");
            Console.WriteLine($"Extracted info: {extractResult}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in text extraction: {ex.Message}");
        }

        // Example 2: Chat agent conversation
        Console.WriteLine("\n--- Example 2: Chat Agent ---");
        var messages = new List<Message>
        {
            new Message { Role = "assistant", Content = "How can I help you today?" }
        };

        try
        {
            // Simulate a conversation
            Console.WriteLine($"Assistant: {messages.Last().Content}");

            // Add user message
            messages.Add(new Message { Role = "user", Content = "Tell me a joke!" });
            Console.WriteLine($"User: {messages.Last().Content}");

            // Get response from chat agent
            var response = await client.ChatAgentAsync(messages, "happy");

            if (response is ReplyTool replyTool)
            {
                Console.WriteLine($"Assistant: {replyTool.Response}");
                messages.Add(new Message { Role = "assistant", Content = replyTool.Response });
            }
            else if (response is StopTool stopTool)
            {
                Console.WriteLine($"Assistant decided to stop: {stopTool.Action}");
            }
            else
            {
                Console.WriteLine($"Assistant: {response}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in chat: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
