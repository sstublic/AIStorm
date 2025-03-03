using AIStorm.Core.Models;
using AIStorm.Core.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIStorm.Core.IntegrationTests;

public class OpenAITests
{
    private readonly IAIProvider aiProvider;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly ILogger<OpenAITests> logger;

    public OpenAITests(IAIProvider aiProvider, ILogger<OpenAITests> logger)
    {
        this.aiProvider = aiProvider;
        this.logger = logger;
        this.jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task RunAllTests()
    {
        logger.LogInformation("Starting OpenAI integration tests");
        
        try
        {
            // Test 1: Get available models
            await TestListModels();
            
            // Test 2: Send a simple message
            await TestSendMessage();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during tests: {Message}", ex.Message);
        }
    }

    private async Task TestListModels()
    {
        logger.LogInformation("Test: Listing available OpenAI models");
        
        try
        {
            var models = await aiProvider.GetAvailableModelsAsync();
            
            logger.LogInformation("Successfully retrieved {Count} models", models.Length);
            foreach (var model in models.Take(5)) // Show just the first 5 models
            {
                logger.LogInformation("  - {Model}", model);
            }
            
            if (models.Length > 5)
            {
                logger.LogInformation("  - ... and {Count} more", models.Length - 5);
            }
            
            logger.LogInformation("✅ Model listing test passed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Model listing test failed: {Message}", ex.Message);
            throw;
        }
    }

    private async Task TestSendMessage()
    {
        logger.LogInformation("Test: Sending a message to OpenAI");
        
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
            var conversationHistory = new List<StormMessage>();
            
            // Send a test message
            string userMessage = "What is the capital of France?";
            logger.LogInformation("Sending message: \"{Message}\"", userMessage);
            
            var response = await aiProvider.SendMessageAsync(agent, conversationHistory, userMessage);
            
            logger.LogInformation("Response received: \"{Response}\"", response);
            
            if (!string.IsNullOrEmpty(response))
            {
                logger.LogInformation("✅ Message sending test passed");
            }
            else
            {
                throw new Exception("Response was empty");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Message sending test failed: {Message}", ex.Message);
            throw;
        }
    }
}
