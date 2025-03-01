using AIStorm.Core.Models;
using AIStorm.Core.Services;
using System.IO;

namespace Core.Tests.Services;

public class MarkdownStorageProviderPremiseTests
{
    private readonly string testBasePath;
    private readonly IStorageProvider storageProvider;

    public MarkdownStorageProviderPremiseTests()
    {
        testBasePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "SessionExample");
        
        if (!Directory.Exists(testBasePath))
        {
            throw new DirectoryNotFoundException($"Test data directory not found: {testBasePath}");
        }
        
        storageProvider = new MarkdownStorageProvider(testBasePath);
    }

    [Fact]
    public void LoadSessionPremise_ValidFile_ReturnsSessionPremise()
    {
        // Arrange
        var premisePath = "SessionExample.ini.md";

        // Act
        var premise = storageProvider.LoadSessionPremise(premisePath);

        // Assert
        Assert.NotNull(premise);
        Assert.Equal("SessionExample", premise.Id);
        Assert.Contains("example session premise", premise.Content);
    }

    [Fact]
    public void SaveSessionPremise_ValidPremise_CreatesFile()
    {
        // Arrange
        var sessionId = "TestPremise";
        var premisePath = sessionId + ".ini.md";
        var fullPath = Path.Combine(testBasePath, premisePath);
        
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
            storageProvider.SaveSessionPremise(premisePath, premise);

            // Assert
            Assert.True(File.Exists(fullPath));
            var content = File.ReadAllText(fullPath);
            Assert.Contains("<aistorm />", content);
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
        var premisePath = sessionId + ".ini.md";
        var fullPath = Path.Combine(testBasePath, premisePath);
        
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
            storageProvider.SaveSessionPremise(premisePath, originalPremise);
            var loadedPremise = storageProvider.LoadSessionPremise(premisePath);

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
