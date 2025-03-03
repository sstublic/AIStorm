using AIStorm.Core.Models;
using AIStorm.Core.Storage;
using AIStorm.Core.Storage.Markdown;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using Moq;

namespace Core.Tests.Services;

public class MarkdownStorageProviderTests
{
    private readonly string testBasePath;
    private readonly IStorageProvider storageProvider;

    public MarkdownStorageProviderTests()
    {
        // Use the TestData directory for test data
        // The files are copied to the output directory during build
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
    public void LoadAgent_ValidFile_ReturnsAgent()
    {
        // Arrange
        var agentName = "Creative Thinker";

        // Act
        var agent = storageProvider.LoadAgent(agentName);

        // Assert
        Assert.NotNull(agent);
        Assert.Equal(agentName, agent.Name);
        Assert.Equal("OpenAI", agent.AIServiceType);
        Assert.Equal("gpt-4o", agent.AIModel);
        Assert.Contains("You are a creative thinking expert", agent.SystemPrompt);
    }

    [Fact]
    public void SaveAgent_ValidAgent_CreatesFile()
    {
        // Arrange
        var agentName = "TestSavedAgent";
        var fullPath = Path.Combine(testBasePath, "AgentTemplates", agentName + ".md");
        
        // Clean up any existing test file
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        var agent = new Agent(agentName, "OpenAI", "gpt-4o", "This is a saved test agent.");

        try
        {
            // Act
            storageProvider.SaveAgent(agentName, agent);

            // Assert
            Assert.True(File.Exists(fullPath));
            var content = File.ReadAllText(fullPath);
            Assert.Contains("<aistorm type=\"agent\" name=\"TestSavedAgent\" service=\"OpenAI\" model=\"gpt-4o\" />", content);
            Assert.Contains("This is a saved test agent.", content);
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
    public void LoadAgent_SavedAgent_RoundTrip()
    {
        // Arrange
        var agentName = "TestRoundTripAgent";
        var fullPath = Path.Combine(testBasePath, "AgentTemplates", agentName + ".md");
        
        // Clean up any existing test file
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        
        // Use literal strings for metadata values
        var originalAgent = new Agent(agentName, "OpenAI", "gpt-4o", "This is a round trip test agent.");

        try
        {
            // Act
            storageProvider.SaveAgent(agentName, originalAgent);
            var loadedAgent = storageProvider.LoadAgent(agentName);

            // Assert
            Assert.NotNull(loadedAgent);
            Assert.Equal(originalAgent.Name, loadedAgent.Name);
            Assert.Equal(originalAgent.AIServiceType, loadedAgent.AIServiceType);
            Assert.Equal(originalAgent.AIModel, loadedAgent.AIModel);
            Assert.Equal(originalAgent.SystemPrompt, loadedAgent.SystemPrompt);
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
