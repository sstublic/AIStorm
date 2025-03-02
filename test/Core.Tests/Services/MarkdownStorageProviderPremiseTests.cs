using AIStorm.Core.Models;
using AIStorm.Core.Services;
using AIStorm.Core.Services.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
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
    public void LoadSessionPremise_ValidFile_ReturnsSessionPremise()
    {
        // Arrange - we'll load from the session file instead
        var sessionPath = "SessionExample.session.md";

        // Act
        var premise = storageProvider.LoadSessionPremise(sessionPath);

        // Assert
        Assert.NotNull(premise);
        Assert.Equal("SessionExample", premise.Id);
        Assert.Contains("brainstorming session about weekend projects", premise.Content);
    }

    [Fact]
    public void SaveSessionPremise_ValidPremise_CreatesFile()
    {
        // Arrange
        var sessionId = "TestPremise";
        var sessionPath = sessionId + ".session.md";
        var fullPath = Path.Combine(testBasePath, "Sessions", sessionPath);
        
        // Clean up any existing test file
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        
        var premise = new SessionPremise(
            sessionId,
            "This is a test session premise."
        );

        try
        {
            // Act
            storageProvider.SaveSessionPremise(sessionPath, premise);

            // Assert
            Assert.True(File.Exists(fullPath));
            var content = File.ReadAllText(fullPath);
            // Since it creates a self-contained session now
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
    public void LoadSessionPremise_SavedPremise_RoundTrip()
    {
        // Arrange
        var sessionId = "TestRoundTripPremise";
        var sessionPath = sessionId + ".session.md";
        var fullPath = Path.Combine(testBasePath, "Sessions", sessionPath);
        
        // Clean up any existing test file
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        
        var originalPremise = new SessionPremise(
            sessionId,
            "This is a round trip test session premise."
        );

        try
        {
            // Act
            storageProvider.SaveSessionPremise(sessionPath, originalPremise);
            var loadedPremise = storageProvider.LoadSessionPremise(sessionPath);

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
