using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AIStorm.Core.Services;
using AIStorm.Core.Models;
using AIStorm.Core.Models.AI;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIStorm.Core.IntegrationTests;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Set up the host with configuration
        using var host = CreateHostBuilder(args).Build();
        
        // Run the tests
        var openAITests = host.Services.GetRequiredService<OpenAITests>();
        await openAITests.RunAllTests();
        
        Console.WriteLine("\nAll tests completed!");
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                      .AddUserSecrets<Program>()
                      .AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Add AIStorm Core services
                services.AddAIStormCore(context.Configuration);
                
                // Register the test class
                services.AddSingleton<OpenAITests>();
            });
}

/// <summary>
/// Contains integration tests for the OpenAI provider.
/// </summary>
public class OpenAITests
{
    private readonly IAIProvider aiProvider;
    private readonly JsonSerializerOptions jsonOptions;

    public OpenAITests(IAIProvider aiProvider)
    {
        this.aiProvider = aiProvider;
        this.jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task RunAllTests()
    {
        Console.WriteLine("Starting OpenAI integration tests...\n");
        
        try
        {
            // Test 1: Get available models
            await TestListModels();
            
            // Test 2: Send a simple message
            await TestSendMessage();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during tests: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private async Task TestListModels()
    {
        Console.WriteLine("Test: Listing available OpenAI models");
        
        try
        {
            var models = await aiProvider.GetAvailableModelsAsync();
            
            Console.WriteLine($"Successfully retrieved {models.Length} models:");
            foreach (var model in models.Take(5)) // Show just the first 5 models
            {
                Console.WriteLine($"  - {model}");
            }
            
            if (models.Length > 5)
            {
                Console.WriteLine($"  - ... and {models.Length - 5} more");
            }
            
            Console.WriteLine("✅ Model listing test passed\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Model listing test failed: {ex.Message}");
            throw;
        }
    }

    private async Task TestSendMessage()
    {
        Console.WriteLine("Test: Sending a message to OpenAI");
        
        try
        {
            // Create a test agent
            var agent = new Agent(
                "TestAgent",
                "OpenAI",
                "gpt-3.5-turbo", 
                "You are a helpful assistant that provides concise responses."
            );
            
            // Create an empty conversation history
            var conversationHistory = new List<AIStorm.Core.Models.AI.Message>();
            
            // Send a test message
            string userMessage = "What is the capital of France?";
            Console.WriteLine($"Sending message: \"{userMessage}\"");
            
            var response = await aiProvider.SendMessageAsync(agent, conversationHistory, userMessage);
            
            Console.WriteLine("Response received:");
            Console.WriteLine($"  \"{response}\"\n");
            
            if (!string.IsNullOrEmpty(response))
            {
                Console.WriteLine("✅ Message sending test passed\n");
            }
            else
            {
                throw new Exception("Response was empty");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Message sending test failed: {ex.Message}");
            throw;
        }
    }
}
