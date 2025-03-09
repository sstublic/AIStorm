using AIStorm.Core.Models;
using AIStorm.Core.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIStorm.Core.IntegrationTests;

public class AnthropicTests
{
    private readonly IAIProvider anthropicProvider;
    private readonly ILogger<AnthropicTests> logger;

    public AnthropicTests(AIProviderManager providerManager, ILogger<AnthropicTests> logger)
    {
        this.anthropicProvider = providerManager.GetProviderByName("Anthropic");
        this.logger = logger;
        
        logger.LogInformation("AnthropicTests initialized with provider type: {ProviderType}", 
            anthropicProvider.GetType().Name);
    }

    public async Task RunTest()
    {
        logger.LogInformation("Starting Anthropic integration tests");
        
        try
        {
            // Test with Claude 3.5 model (no thinking)
            await TestClaude35();
            
            // Test with Claude 3.7 model (with thinking)
            await TestClaude37();
            
            logger.LogInformation("Anthropic tests completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during Anthropic tests: {Message}", ex.Message);
            throw;
        }
    }

    private async Task TestClaude35()
    {
        logger.LogInformation("Test: Sending a message to Claude 3.5 (no thinking capability)");
        
        try
        {
            // Create a test agent with Claude 3.5 model (no thinking)
            var agent = new Agent(
                "TestAgent35",
                "Anthropic",
                "claude-3-5-sonnet-20240620", 
                "You are a helpful assistant that provides concise responses."
            );
            
            await SendTestMessage(agent, "What is the capital of France?");
            
            logger.LogInformation("✅ Claude 3.5 test passed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Claude 3.5 test failed: {Message}", ex.Message);
            throw;
        }
    }
    
    private async Task TestClaude37()
    {
        logger.LogInformation("Test: Sending a message to Claude 3.7 (with thinking capability)");
        
        try
        {
            // Create a test agent with Claude 3.7 model (with thinking)
            var agent = new Agent(
                "TestAgent37",
                "Anthropic",
                "claude-3-7-sonnet-20250219", 
                "You are a helpful assistant that provides concise responses."
            );
            
            await SendTestMessage(agent, "Please explain quantum computing in simple terms.");
            
            logger.LogInformation("✅ Claude 3.7 test with thinking passed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Claude 3.7 test failed: {Message}", ex.Message);
            throw;
        }
    }
    
    private async Task<string> SendTestMessage(Agent agent, string messageText)
    {
        logger.LogInformation("Sending message to {Model}: \"{Message}\"", agent.AIModel, messageText);
        
        // Create an empty conversation history
        var conversationHistory = new List<StormMessage>();
        
        // Create a session premise
        var premise = new SessionPremise("TestSession", "This is a test premise");
        
        // Add the user message to the conversation history
        conversationHistory.Add(new StormMessage("user", DateTime.UtcNow, messageText));
        
        var response = await anthropicProvider.SendMessageAsync(agent, premise, conversationHistory);
        
        logger.LogInformation("Response from {Model} received: \"{Response}\"", agent.AIModel, response);
        
        if (string.IsNullOrEmpty(response))
        {
            throw new Exception("Response was empty");
        }
        
        return response;
    }
}
