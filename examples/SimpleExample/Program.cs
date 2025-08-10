using Baml.Runtime;
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
