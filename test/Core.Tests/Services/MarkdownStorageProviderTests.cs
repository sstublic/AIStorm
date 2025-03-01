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
        testBasePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
        
        if (!Directory.Exists(testBasePath))
        {
            Directory.CreateDirectory(testBasePath);
        }
        
        storageProvider = new MarkdownStorageProvider(testBasePath);
    }

    [Fact]
    public void LoadAgent_ValidFile_ReturnsAgent()
    {
        // Arrange
        var agentName = "TestAgent";
        var agentPath = Path.Combine("Agents", $"{agentName}.md");
        var fullPath = Path.Combine(testBasePath, agentPath);
        var directoryPath = Path.GetDirectoryName(fullPath);
        
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var agentContent = @"<aistorm type=""OpenAI"" model=""gpt-4"" />

# TestAgent

This is a test agent for unit testing.";

        File.WriteAllText(fullPath, agentContent);

        // Act
        var agent = storageProvider.LoadAgent(agentPath);

        // Assert
        Assert.NotNull(agent);
        Assert.Equal(agentName, agent.Name);
        Assert.Equal("OpenAI", agent.AIServiceType);
        Assert.Equal("gpt-4", agent.AIModel);
        Assert.Contains("This is a test agent for unit testing.", agent.SystemPrompt);
    }

    [Fact]
    public void SaveAgent_ValidAgent_CreatesFile()
    {
        // Arrange
        var agentName = "SavedAgent";
        var agentPath = Path.Combine("Agents", $"{agentName}.md");
        var fullPath = Path.Combine(testBasePath, agentPath);
        
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        var agent = new Agent(agentName, "OpenAI", "gpt-4", "This is a saved test agent.");

        // Act
        storageProvider.SaveAgent(agentPath, agent);

        // Assert
        Assert.True(File.Exists(fullPath));
        var content = File.ReadAllText(fullPath);
        Assert.Contains("<aistorm type=\"OpenAI\" model=\"gpt-4\" />", content);
        Assert.Contains("# SavedAgent", content);
        Assert.Contains("This is a saved test agent.", content);
    }

    [Fact]
    public void LoadAgent_SavedAgent_RoundTrip()
    {
        // Arrange
        var agentName = "RoundTripAgent";
        var agentPath = Path.Combine("Agents", $"{agentName}.md");
        var originalAgent = new Agent(agentName, "OpenAI", "gpt-4", "This is a round trip test agent.");

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
}
