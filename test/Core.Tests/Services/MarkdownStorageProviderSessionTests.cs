using AIStorm.Core.Models;
using AIStorm.Core.Services;
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
        testBasePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "SessionExample");
        
        if (!Directory.Exists(testBasePath))
        {
            throw new DirectoryNotFoundException($"Test data directory not found: {testBasePath}");
        }
        
        storageProvider = new MarkdownStorageProvider(testBasePath);
    }

    [Fact]
    public void LoadSession_ValidFile_ReturnsSession()
    {
        // Arrange
        var sessionPath = "SessionExample.md";

        // Act
        var session = storageProvider.LoadSession(sessionPath);

        // Assert
        Assert.NotNull(session);
        Assert.Equal("SessionExample", session.Id);
        Assert.Equal("Simple example brainstorming session", session.Description);
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
    public void SaveSession_ValidSession_CreatesFile()
    {
        // Arrange
        var sessionId = "TestSession";
        var sessionPath = sessionId + ".md";
        var fullPath = Path.Combine(testBasePath, sessionPath);
        
        // Clean up any existing test file
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        
        var session = new Session(
            sessionId,
            Tools.ParseAsUtc("2025-03-01T15:00:00"),
            "Test Session"
        );
        
        session.Messages.Add(new Message(
            "user",
            Tools.ParseAsUtc("2025-03-01T15:01:00"),
            "Test message from user."
        ));
        
        session.Messages.Add(new Message(
            "agent",
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
            Assert.Contains("<aistorm type=\"message\" from=\"user\" timestamp=\"2025-03-01T15:01:00\" />", content);
            Assert.Contains("## [user]:", content);
            Assert.Contains("Test message from user.", content);
            Assert.Contains("<aistorm type=\"message\" from=\"agent\" timestamp=\"2025-03-01T15:02:00\" />", content);
            Assert.Contains("## [agent]:", content);
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
    public void LoadSession_SavedSession_RoundTrip()
    {
        // Arrange
        var sessionId = "TestRoundTripSession";
        var sessionPath = sessionId + ".md";
        var fullPath = Path.Combine(testBasePath, sessionPath);
        
        // Clean up any existing test file
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        
        var originalSession = new Session(
            sessionId,
            Tools.ParseAsUtc("2025-03-01T15:00:00"),
            "Round Trip Test Session"
        );
        
        originalSession.Messages.Add(new Message(
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
