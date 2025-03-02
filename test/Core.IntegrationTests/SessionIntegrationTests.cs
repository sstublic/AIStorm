using AIStorm.Core.Models;
using AIStorm.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AIStorm.Core.IntegrationTests;

public class SessionIntegrationTests
{
    private readonly IStorageProvider storageProvider;
    private readonly ISessionRunnerFactory sessionRunnerFactory;
    private readonly ILogger<SessionIntegrationTests> logger;

    public SessionIntegrationTests(
        IStorageProvider storageProvider, 
        ISessionRunnerFactory sessionRunnerFactory, 
        ILogger<SessionIntegrationTests> logger)
    {
        this.storageProvider = storageProvider;
        this.sessionRunnerFactory = sessionRunnerFactory;
        this.logger = logger;
    }

    public async Task RunTest()
    {
        logger.LogInformation("Starting AIStorm Session Integration Test");
        logger.LogInformation("This test demonstrates a session with multiple agents exchanging messages");
        
        try
        {
            // Load agents from test files
            logger.LogInformation("Loading agents from test files");
            var creativeAgent = storageProvider.LoadAgent("SessionExample/Agents/Creative Thinker.md");
            var criticalAgent = storageProvider.LoadAgent("SessionExample/Agents/Critical Analyst.md");
            
            logger.LogInformation("Loaded agent: {AgentName} ({ServiceType}, {ModelName})", 
                creativeAgent.Name, creativeAgent.AIServiceType, creativeAgent.AIModel);
            logger.LogInformation("Loaded agent: {AgentName} ({ServiceType}, {ModelName})", 
                criticalAgent.Name, criticalAgent.AIServiceType, criticalAgent.AIModel);
            
            // Load session with premise
            logger.LogInformation("Loading session with premise");
            var session = storageProvider.LoadSession("SessionExample");
            var premise = session.Premise;
            logger.LogInformation("Loaded premise: {Content}", premise.Content);
            
            // Initialize session runner with agents and premise
            var agents = new List<Agent> { creativeAgent, criticalAgent };
            var sessionRunner = sessionRunnerFactory.Create(agents, premise);
            
            logger.LogInformation("----------------------------------------");
            logger.LogInformation("Starting conversation");
            
            // Add initial user message
            string initialQuestion = "What are some interesting weekend projects I could work on?";
            logger.LogInformation("[Human] ({Timestamp}): {Message}", 
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), initialQuestion);
            sessionRunner.AddUserMessage(initialQuestion);
            
            // First agent response
            logger.LogInformation("Waiting for Creative Thinker response");
            await sessionRunner.Next();
            DisplayLastMessage(sessionRunner);
            
            // Second agent response
            logger.LogInformation("Waiting for Critical Analyst response");
            await sessionRunner.Next();
            DisplayLastMessage(sessionRunner);
            
            // User intervention
            string userIntervention = "I'm particularly interested in technology projects that can be completed in a single weekend.";
            logger.LogInformation("----------------------------------------");
            logger.LogInformation("User intervenes in the conversation");
            logger.LogInformation("[Human] ({Timestamp}): {Message}", 
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), userIntervention);
            sessionRunner.AddUserMessage(userIntervention);
            
            // Third agent response
            logger.LogInformation("Waiting for Creative Thinker response");
            await sessionRunner.Next();
            DisplayLastMessage(sessionRunner);
            
            // Fourth agent response
            logger.LogInformation("Waiting for Critical Analyst response");
            await sessionRunner.Next();
            DisplayLastMessage(sessionRunner);
            
            // Display full conversation summary
            logger.LogInformation("----------------------------------------");
            logger.LogInformation("Conversation Summary");
            logger.LogInformation("----------------------------------------");
            
            var allMessages = sessionRunner.GetConversationHistory();
            foreach (var message in allMessages)
            {
                logger.LogInformation("{Content}", message.Content);
            }
            
            logger.LogInformation("----------------------------------------");
            logger.LogInformation("Integration test completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during test: {Message}", ex.Message);
        }
    }
    
    private void DisplayLastMessage(SessionRunner sessionRunner)
    {
        var messages = sessionRunner.GetConversationHistory();
        var lastMessage = messages[messages.Count - 1];
        
        logger.LogInformation("[{AgentName}] ({Timestamp}): {Content}", 
            lastMessage.AgentName, 
            lastMessage.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"), 
            lastMessage.Content);
    }
}
