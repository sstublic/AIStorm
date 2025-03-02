using AIStorm.Core.Models;
using AIStorm.Core.Services;
using AIStorm.Core.Services.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.IO;
using System.Linq;

namespace Core.Tests.Services;

public class MarkdownStorageProviderSessionTests
{
    private readonly string testBasePath;
    private readonly IStorageProvider storageProvider;

    public MarkdownStorageProviderSessionTests()
    {
        testBasePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
        
        if (!Directory.Exists(testBasePath))
        {
            throw new DirectoryNotFoundException($"Test data directory not found: {testBasePath}");
        }
        
        var options = Options.Create(new MarkdownStorageOptions { BasePath = testBasePath });
        var loggerMock = new Mock<ILogger<MarkdownStorageProvider>>();
        storageProvider = new MarkdownStorageProvider(options, new MarkdownSerializer(), loggerMock.Object);
    }

    [Fact]
    public void LoadSession_ValidFile_ReturnsSessionWithAgentsAndPremise()
    {
        // Arrange
        var sessionPath = "SessionExample.session.md";

        // Act
        var session = storageProvider.LoadSession(sessionPath);

        // Assert
        Assert.NotNull(session);
        Assert.Equal("SessionExample", session.Id);
        Assert.Equal("Simple example brainstorming session", session.Description);
        
        // Verify premise was loaded
        Assert.NotNull(session.Premise);
        Assert.Equal("SessionExample", session.Premise.Id);
        Assert.Contains("brainstorming session about weekend projects", session.Premise.Content);
        
        // Verify agents were loaded
        Assert.Equal(3, session.Agents.Count);
        
        // Verify first agent
        var creativeAgent = session.Agents.FirstOrDefault(a => a.Name == "Creative Thinker");
        Assert.NotNull(creativeAgent);
        Assert.Equal("OpenAI", creativeAgent.AIServiceType);
        Assert.Equal("gpt-4o", creativeAgent.AIModel);
        Assert.Contains("creative thinking expert", creativeAgent.SystemPrompt);
        
        // Verify messages were loaded
        Assert.Equal(5, session.Messages.Count);
        
        // Check first message
        var firstMessage = session.Messages.First();
        Assert.Equal("user", firstMessage.AgentName);
        Assert.Contains("What are some ideas for a weekend project?", firstMessage.Content);
        
        // Check last message
        var lastMessage = session.Messages.Last();
        Assert.Equal("user", lastMessage.AgentName);
        Assert.Contains("I like the herb garden idea", lastMessage.Content);
    }

    [Fact]
    public void SaveSession_ValidSessionWithAgentsAndPremise_CreatesFile()
    {
        // Arrange
        var sessionId = "TestSession";
        var sessionPath = sessionId + ".session.md";
        var fullPath = Path.Combine(testBasePath, "Sessions", sessionPath);
        
        // Clean up any existing test file
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        
        var premise = new SessionPremise(sessionId, "This is a test premise for the session.");
        var session = new Session(
            sessionId,
            Tools.ParseAsUtc("2025-03-01T15:00:00"),
            "Test Session",
            premise
        );
        
        // Add test agents
        session.Agents.Add(new Agent(
            "Test Agent",
            "OpenAI",
            "gpt-4o",
            "You are a test agent for checking the session serialization."
        ));
        
        session.Messages.Add(new StormMessage(
            "user",
            Tools.ParseAsUtc("2025-03-01T15:01:00"),
            "Test message from user."
        ));
        
        session.Messages.Add(new StormMessage(
            "Test Agent",
            Tools.ParseAsUtc("2025-03-01T15:02:00"),
            "Test response from agent."
        ));

        try
        {
            // Act
            storageProvider.SaveSession(sessionPath, session);

            // Assert
            Assert.True(File.Exists(fullPath));
            var content = File.ReadAllText(fullPath);
            Assert.Contains("<aistorm type=\"session\" created=\"2025-03-01T15:00:00\" description=\"Test Session\" />", content);
            Assert.Contains("# Test Session", content);
            
            // Check premise was saved
            Assert.Contains("<aistorm type=\"premise\" />", content);
            Assert.Contains("This is a test premise for the session.", content);
            
            // Check agent was saved
            Assert.Contains("<aistorm type=\"agent\" name=\"Test Agent\" service=\"OpenAI\" model=\"gpt-4o\" />", content);
            Assert.Contains("You are a test agent for checking the session serialization.", content);
            
            // Check messages were saved
            Assert.Contains("<aistorm type=\"message\" from=\"user\" timestamp=\"2025-03-01T15:01:00\" />", content);
            Assert.Contains("## [user]:", content);
            Assert.Contains("Test message from user.", content);
            Assert.Contains("<aistorm type=\"message\" from=\"Test Agent\" timestamp=\"2025-03-01T15:02:00\" />", content);
            Assert.Contains("## [Test Agent]:", content);
            Assert.Contains("Test response from agent.", content);
        }
        finally
        {
            // Clean up
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            
            // Clean up directory if it was created
            var directoryPath = Path.Combine(testBasePath, sessionId);
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }
    }

    [Fact]
    public void LoadSession_SavedSessionWithAgentsAndPremise_RoundTrip()
    {
        // Arrange
        var sessionId = "TestRoundTripSession";
        var sessionPath = sessionId + ".session.md";
        var fullPath = Path.Combine(testBasePath, "Sessions", sessionPath);
        
        // Clean up any existing test file
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        
        var premise = new SessionPremise(sessionId, "This is a test premise for the round trip session.");
        var originalSession = new Session(
            sessionId,
            Tools.ParseAsUtc("2025-03-01T15:00:00"),
            "Round Trip Test Session",
            premise
        );
        
        // Add test agent
        var agent = new Agent(
            "Round Trip Agent",
            "OpenAI",
            "gpt-4o",
            "You are a test agent for checking the round trip session serialization."
        );
        originalSession.Agents.Add(agent);
        
        originalSession.Messages.Add(new StormMessage(
            "user",
            Tools.ParseAsUtc("2025-03-01T15:01:00"),
            "Round trip test message."
        ));

        try
        {
            // Act
            storageProvider.SaveSession(sessionPath, originalSession);
            var loadedSession = storageProvider.LoadSession(sessionPath);

            // Assert
            Assert.NotNull(loadedSession);
            Assert.Equal(originalSession.Id, loadedSession.Id);
            Assert.Equal(originalSession.Description, loadedSession.Description);
            Assert.Equal(originalSession.Created, loadedSession.Created);
            
            // Verify premise was round-tripped correctly
            Assert.NotNull(loadedSession.Premise);
            Assert.Equal(originalSession.Premise.Id, loadedSession.Premise.Id);
            Assert.Equal(originalSession.Premise.Content, loadedSession.Premise.Content);
            
            // Verify agent was round-tripped correctly
            Assert.Single(loadedSession.Agents);
            var loadedAgent = loadedSession.Agents[0];
            Assert.Equal(agent.Name, loadedAgent.Name);
            Assert.Equal(agent.AIServiceType, loadedAgent.AIServiceType);
            Assert.Equal(agent.AIModel, loadedAgent.AIModel);
            Assert.Equal(agent.SystemPrompt, loadedAgent.SystemPrompt);
            
            // Verify messages were round-tripped correctly
            Assert.Equal(originalSession.Messages.Count, loadedSession.Messages.Count);
            var originalMessage = originalSession.Messages[0];
            var loadedMessage = loadedSession.Messages[0];
            Assert.Equal(originalMessage.AgentName, loadedMessage.AgentName);
            Assert.Equal(originalMessage.Timestamp, loadedMessage.Timestamp);
            Assert.Equal(originalMessage.Content, loadedMessage.Content);
        }
        finally
        {
            // Clean up
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            
            // Clean up directory if it was created
            var directoryPath = Path.Combine(testBasePath, sessionId);
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }
    }
}
