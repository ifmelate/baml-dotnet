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

namespace StreamingExample;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("BAML .NET Streaming Example");
        Console.WriteLine("===========================");

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

        // Example 1: Streaming story generation
        Console.WriteLine("\n--- Example 1: Streaming Story Generation ---");
        Console.WriteLine("Generating a story about space exploration...\n");

        try
        {
            await foreach (var chunk in client.StreamGenerateStoryAsync(
                "A brave astronaut discovers a mysterious signal from deep space",
                "medium"))
            {
                Console.Write(chunk);
                await Task.Delay(50); // Simulate real-time typing effect
            }
            Console.WriteLine("\n\n[Story generation complete]");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in story generation: {ex.Message}");
        }

        // Example 2: Streaming chat conversation
        Console.WriteLine("\n--- Example 2: Streaming Chat ---");
        var messages = new List<Message>
        {
            new Message { Role = "user", Content = "Tell me about artificial intelligence and its future" }
        };

        try
        {
            Console.WriteLine($"User: {messages.Last().Content}");
            Console.Write("Assistant: ");

            var fullResponse = "";
            await foreach (var response in client.StreamStreamingChatAsync(messages, "artificial intelligence"))
            {
                if (response.Content != null)
                {
                    Console.Write(response.Content);
                    fullResponse += response.Content;
                }

                if (response.Finished)
                {
                    Console.WriteLine("\n\n[Response complete]");
                    break;
                }

                await Task.Delay(30); // Simulate real-time streaming
            }

            // Add the complete response to conversation history
            messages.Add(new Message { Role = "assistant", Content = fullResponse });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in streaming chat: {ex.Message}");
        }

        // Example 3: Interactive streaming chat
        Console.WriteLine("\n--- Example 3: Interactive Chat (type 'quit' to exit) ---");

        while (true)
        {
            Console.Write("\nYou: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrEmpty(userInput) || userInput.ToLower() == "quit")
                break;

            messages.Add(new Message { Role = "user", Content = userInput });

            try
            {
                Console.Write("Assistant: ");
                var fullResponse = "";

                await foreach (var response in client.StreamStreamingChatAsync(messages, "general conversation"))
                {
                    if (response.Content != null)
                    {
                        Console.Write(response.Content);
                        fullResponse += response.Content;
                    }

                    if (response.Finished)
                        break;

                    await Task.Delay(30);
                }

                Console.WriteLine();
                messages.Add(new Message { Role = "assistant", Content = fullResponse });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        Console.WriteLine("\nGoodbye!");
    }
}
