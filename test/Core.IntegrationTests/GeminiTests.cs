using AIStorm.Core.Models;
using AIStorm.Core.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIStorm.Core.IntegrationTests;

public class GeminiTests
{
    private readonly IAIProvider geminiProvider;
    private readonly ILogger<GeminiTests> logger;

    public GeminiTests(AIProviderManager providerManager, ILogger<GeminiTests> logger)
    {
        this.geminiProvider = providerManager.GetProviderByName("Gemini");
        this.logger = logger;
        
        logger.LogInformation("GeminiTests initialized with provider type: {ProviderType}", 
            geminiProvider.GetType().Name);
    }

    public async Task RunTest()
    {
        logger.LogInformation("Starting Gemini integration test");
        
        try
        {
            await TestSendMessage();
            logger.LogInformation("Gemini test completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during Gemini test: {Message}", ex.Message);
            throw;
        }
    }

    private async Task TestSendMessage()
    {
        logger.LogInformation("Test: Sending a message to Gemini");
        
        try
        {
            // Create a test agent
            var agent = new Agent(
                "TestAgent",
                "Gemini",
                "gemini-2.0-flash", 
                "You are a helpful assistant that provides concise responses."
            );
            
            // Create an empty conversation history
            var conversationHistory = new List<StormMessage>();
            
            // Send a test message
            string userMessage = "What is the capital of France?";
            logger.LogInformation("Sending message: \"{Message}\"", userMessage);
            
            // Create a session premise
            var premise = new SessionPremise("TestSession", "This is a test premise");
            
            // Add the user message to the conversation history
            conversationHistory.Add(new StormMessage("user", DateTime.UtcNow, userMessage));
            
            var response = await geminiProvider.SendMessageAsync(agent, premise, conversationHistory);
            
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
