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
        Console.WriteLine("Starting AIStorm Session Integration Test...\n");
        Console.WriteLine("This test demonstrates a session with multiple agents exchanging messages\n");
        
        try
        {
            // Load agents from test files
            Console.WriteLine("Loading agents from test files...");
            var creativeAgent = storageProvider.LoadAgent("SessionExample/Agents/Creative Thinker.md");
            var criticalAgent = storageProvider.LoadAgent("SessionExample/Agents/Critical Analyst.md");
            
            Console.WriteLine($"Loaded agent: {creativeAgent.Name} ({creativeAgent.AIServiceType}, {creativeAgent.AIModel})");
            Console.WriteLine($"Loaded agent: {criticalAgent.Name} ({criticalAgent.AIServiceType}, {criticalAgent.AIModel})\n");
            
            // Load session premise
            Console.WriteLine("Loading session premise...");
            var premise = storageProvider.LoadSessionPremise("SessionExample/SessionExample.ini.md");
            Console.WriteLine($"Loaded premise: {premise.Content}\n");
            
            // Initialize session runner with agents and premise
            var agents = new List<Agent> { creativeAgent, criticalAgent };
            var sessionRunner = sessionRunnerFactory.Create(agents, premise);
            
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("Starting conversation\n");
            
            // Add initial user message
            string initialQuestion = "What are some interesting weekend projects I could work on?";
            Console.WriteLine($"[Human] ({DateTime.Now:yyyy-MM-dd HH:mm:ss}):");
            Console.WriteLine(initialQuestion);
            Console.WriteLine();
            sessionRunner.AddUserMessage(initialQuestion);
            
            // First agent response
            Console.WriteLine("Waiting for Creative Thinker response...");
            await sessionRunner.Next();
            DisplayLastMessage(sessionRunner);
            
            // Second agent response
            Console.WriteLine("Waiting for Critical Analyst response...");
            await sessionRunner.Next();
            DisplayLastMessage(sessionRunner);
            
            // User intervention
            string userIntervention = "I'm particularly interested in technology projects that can be completed in a single weekend.";
            Console.WriteLine("\n----------------------------------------");
            Console.WriteLine("User intervenes in the conversation\n");
            Console.WriteLine($"[Human] ({DateTime.Now:yyyy-MM-dd HH:mm:ss}):");
            Console.WriteLine(userIntervention);
            Console.WriteLine();
            sessionRunner.AddUserMessage(userIntervention);
            
            // Third agent response
            Console.WriteLine("Waiting for Creative Thinker response...");
            await sessionRunner.Next();
            DisplayLastMessage(sessionRunner);
            
            // Fourth agent response
            Console.WriteLine("Waiting for Critical Analyst response...");
            await sessionRunner.Next();
            DisplayLastMessage(sessionRunner);
            
            // Display full conversation summary
            Console.WriteLine("\n----------------------------------------");
            Console.WriteLine("Conversation Summary");
            Console.WriteLine("----------------------------------------\n");
            
            var allMessages = sessionRunner.GetConversationHistory();
            foreach (var message in allMessages)
            {
                Console.WriteLine(message.Content);
                Console.WriteLine();
            }
            
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("Integration test completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during test: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
    
    private void DisplayLastMessage(SessionRunner sessionRunner)
    {
        var messages = sessionRunner.GetConversationHistory();
        var lastMessage = messages[messages.Count - 1];
        
        Console.WriteLine($"\n[{lastMessage.AgentName}] ({lastMessage.Timestamp:yyyy-MM-dd HH:mm:ss}):");
        Console.WriteLine(lastMessage.Content);
        Console.WriteLine();
    }
}
