using AIStorm.Core.Models;
using AIStorm.Core.Services;
using System.IO;

namespace Core.Tests.Services;

public class MarkdownStorageProviderTests
{
    private readonly string testBasePath;
    private readonly IStorageProvider storageProvider;

    public MarkdownStorageProviderTests()
    {
        // Use the TestData/SessionExample directory for test data
        // The files are copied to the output directory during build
        testBasePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "SessionExample");
        
        if (!Directory.Exists(testBasePath))
        {
            throw new DirectoryNotFoundException($"Test data directory not found: {testBasePath}");
        }
        
        storageProvider = new MarkdownStorageProvider(testBasePath);
    }

    [Fact]
    public void LoadAgent_ValidFile_ReturnsAgent()
    {
        // Arrange
        var agentName = "Creative Thinker";
        var agentPath = Path.Combine("Agents", $"{agentName}.md");

        // Act
        var agent = storageProvider.LoadAgent(agentPath);

        // Assert
        Assert.NotNull(agent);
        Assert.Equal(agentName, agent.Name);
        Assert.Equal("OpenAI", agent.AIServiceType);
        Assert.Equal("gpt-4", agent.AIModel);
        Assert.Contains("You are a creative thinking expert", agent.SystemPrompt);
    }

    [Fact]
    public void SaveAgent_ValidAgent_CreatesFile()
    {
        // Arrange
        var agentName = "TestSavedAgent";
        var agentPath = Path.Combine("Agents", $"{agentName}.md");
        var fullPath = Path.Combine(testBasePath, agentPath);
        
        // Clean up any existing test file
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        var agent = new Agent(agentName, "OpenAI", "gpt-4", "This is a saved test agent.");

        try
        {
            // Act
            storageProvider.SaveAgent(agentPath, agent);

            // Assert
            Assert.True(File.Exists(fullPath));
            var content = File.ReadAllText(fullPath);
            Assert.Contains("<aistorm type=\"OpenAI\" model=\"gpt-4\" />", content);
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
        var agentPath = Path.Combine("Agents", $"{agentName}.md");
        var fullPath = Path.Combine(testBasePath, agentPath);
        
        // Clean up any existing test file
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        
        // Use literal strings for metadata values
        var originalAgent = new Agent(agentName, "OpenAI", "gpt-4", "This is a round trip test agent.");

        try
        {
            // Act
            storageProvider.SaveAgent(agentPath, originalAgent);
            var loadedAgent = storageProvider.LoadAgent(agentPath);

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
