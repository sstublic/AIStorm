using AIStorm.Core.Models;
using AIStorm.Core.Storage;
using AIStorm.Core.Storage.Markdown;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.IO;

namespace Core.Tests.Services;

public class MarkdownStorageProviderPremiseTests
{
    private readonly string testBasePath;
    private readonly IStorageProvider storageProvider;

    public MarkdownStorageProviderPremiseTests()
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
    public void GetSessionPremise_FromExistingSession_ReturnsPremise()
    {
        // Arrange
        var sessionId = "SessionExample";

        // Act
        var session = storageProvider.LoadSession(sessionId);
        var premise = session.Premise;

        // Assert
        Assert.NotNull(premise);
        Assert.Equal("SessionExample", premise.Id);
        Assert.Contains("brainstorming session about weekend projects", premise.Content);
    }

    [Fact]
    public void SaveSession_WithPremise_CreatesFile()
    {
        // Arrange
        var sessionId = "TestPremise";
        var fullPath = Path.Combine(testBasePath, "Sessions", sessionId + ".session.md");
        
        // Clean up any existing test file
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        
        var premise = new SessionPremise(
            sessionId,
            "This is a test session premise."
        );
        
        var session = new Session(
            sessionId,
            DateTime.UtcNow,
            "Test Session with Premise",
            premise
        );

        try
        {
            // Act
            storageProvider.SaveSession(sessionId, session);

            // Assert
            Assert.True(File.Exists(fullPath));
            var content = File.ReadAllText(fullPath);
            Assert.Contains("<aistorm type=\"premise\" />", content);
            Assert.Contains("This is a test session premise.", content);
        }
        finally
        {
            // Clean up
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }

    [Fact]
    public void SessionPremise_RoundTrip()
    {
        // Arrange
        var sessionId = "TestRoundTripPremise";
        var fullPath = Path.Combine(testBasePath, "Sessions", sessionId + ".session.md");
        
        // Clean up any existing test file
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        
        var originalPremise = new SessionPremise(
            sessionId,
            "This is a round trip test session premise."
        );
        
        var originalSession = new Session(
            sessionId,
            DateTime.UtcNow,
            "Round Trip Session Test",
            originalPremise
        );

        try
        {
            // Act
            storageProvider.SaveSession(sessionId, originalSession);
            var loadedSession = storageProvider.LoadSession(sessionId);
            var loadedPremise = loadedSession.Premise;

            // Assert
            Assert.NotNull(loadedPremise);
            Assert.Equal(originalPremise.Id, loadedPremise.Id);
            Assert.Equal(originalPremise.Content, loadedPremise.Content);
        }
        finally
        {
            // Clean up
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }
}
