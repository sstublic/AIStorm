using AIStorm.Core.Models;
using AIStorm.Core.Storage;
using AIStorm.Core.Storage.Markdown;
using AIStorm.Core.SessionManagement;
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
            // Load self-contained session with embedded agents and premise
            logger.LogInformation("Loading session with embedded agents and premise");
            var session = storageProvider.LoadSession("SessionExample");
            
            logger.LogInformation("Session loaded successfully with ID: {SessionId}", session.Id);
            logger.LogInformation("Session contains {AgentCount} embedded agents", session.Agents.Count);
            
            foreach (var agent in session.Agents)
            {
                logger.LogInformation("Embedded agent: {AgentName} ({ServiceType}, {ModelName})", 
                    agent.Name, agent.AIServiceType, agent.AIModel);
            }
            
            logger.LogInformation("Session premise: {Premise}", session.Premise);
            
            // Initialize session runner with the agents and premise from the loaded session
            var sessionRunner = sessionRunnerFactory.CreateWithNewSession(session.Agents, session.Premise);
            
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
            
            // Save the entire session with a timestamped name
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
            var sessionId = $"IntegrationTest_{timestamp}";
            logger.LogInformation("Saving session as: {SessionId}", sessionId);
            storageProvider.SaveSession(sessionId, sessionRunner.Session);
            logger.LogInformation("Session saved successfully");
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
