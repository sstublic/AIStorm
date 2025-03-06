using AIStorm.Core.AI;
using AIStorm.Core.Models;
using AIStorm.Core.SessionManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Core.IntegrationTests;

public class AIMockProviderTests
{
    private readonly IServiceProvider serviceProvider;
    
    public AIMockProviderTests()
    {
        // Setup service provider
        var services = new ServiceCollection();
        
        // Configure test services
        services.AddLogging(config => 
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Debug);
        });
        
        // Configure AIMock options
        services.Configure<AIMockOptions>(options => 
        {
            options.ApiKey = "not-needed";
            options.Models = new List<string> { "AlwaysThrows", "AlwaysReturns" };
        });
        
        // Add required services
        services.AddSingleton<IPromptBuilder, PromptBuilder>();
        services.AddSingleton<IAIProvider, AIMockProvider>();
        
        serviceProvider = services.BuildServiceProvider();
    }
    
    [Fact]
    public async Task GetAvailableModelsAsync_ReturnsConfiguredModels()
    {
        // Arrange
        var provider = serviceProvider.GetRequiredService<IAIProvider>();
        
        // Act
        var models = await provider.GetAvailableModelsAsync();
        
        // Assert
        Assert.NotNull(models);
        Assert.Equal(2, models.Length);
        Assert.Contains("AlwaysThrows", models);
        Assert.Contains("AlwaysReturns", models);
    }
    
    [Fact]
    public async Task SendMessageAsync_WithAlwaysReturnsModel_ReturnsExpectedResponse()
    {
        // Arrange
        var provider = serviceProvider.GetRequiredService<IAIProvider>();
        var agent = new Agent 
        { 
            Name = "TestAgent", 
            AIModel = "AlwaysReturns",
            SystemPrompt = "Test system prompt"
        };
        var premise = new SessionPremise { Content = "Test premise" };
        var messages = new List<StormMessage>();
        
        // Act
        var response = await provider.SendMessageAsync(agent, premise, messages);
        
        // Assert
        Assert.NotNull(response);
        Assert.Contains("This is a mock response from AIMockProvider's AlwaysReturns model", response);
        Assert.Contains("Agent: TestAgent", response);
        Assert.Contains("Premise: Test premise", response);
        Assert.Contains("Message Count: 0", response);
    }
    
    [Fact]
    public async Task SendMessageAsync_WithAlwaysThrowsModel_ThrowsException()
    {
        // Arrange
        var provider = serviceProvider.GetRequiredService<IAIProvider>();
        var agent = new Agent 
        { 
            Name = "TestAgent", 
            AIModel = "AlwaysThrows",
            SystemPrompt = "Test system prompt"
        };
        var premise = new SessionPremise { Content = "Test premise" };
        var messages = new List<StormMessage>();
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            provider.SendMessageAsync(agent, premise, messages));
        
        Assert.Contains("This is a mock exception from AIMockProvider's AlwaysThrows model", exception.Message);
    }
    
    [Fact]
    public async Task SendMessageAsync_WithInvalidModel_ThrowsArgumentException()
    {
        // Arrange
        var provider = serviceProvider.GetRequiredService<IAIProvider>();
        var agent = new Agent 
        { 
            Name = "TestAgent", 
            AIModel = "InvalidModel",
            SystemPrompt = "Test system prompt"
        };
        var premise = new SessionPremise { Content = "Test premise" };
        var messages = new List<StormMessage>();
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            provider.SendMessageAsync(agent, premise, messages));
        
        Assert.Contains("Unknown model 'InvalidModel' requested in AIMockProvider", exception.Message);
    }
}
