namespace AIStorm.Core.Tests.Services;

using AIStorm.Core.Models;
using AIStorm.Core.AI;
using AIStorm.Core.SessionManagement;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class SessionRunnerTests
{
    private readonly SessionPremise premise;
    private readonly List<Agent> agents;
    private readonly Mock<AIProviderManager> providerManagerMock;
    private readonly Mock<ILogger<SessionRunner>> loggerMock;
    private readonly Mock<IAIProvider> defaultProviderMock;

    public SessionRunnerTests()
    {
        // Set up test data
        premise = new SessionPremise("test-premise", "This is a test premise");
        
        agents = new List<Agent>
        {
            new Agent("Agent1", "OpenAI", "gpt-4", "You are Agent1"),
            new Agent("Agent2", "OpenAI", "gpt-4", "You are Agent2"),
            new Agent("Agent3", "OpenAI", "gpt-4", "You are Agent3")
        };
        
        // Set up mocks
        defaultProviderMock = new Mock<IAIProvider>();
        loggerMock = new Mock<ILogger<SessionRunner>>();
        
        // Create provider manager mock
        providerManagerMock = new Mock<AIProviderManager>(
            new[] { defaultProviderMock.Object },
            new Mock<ILogger<AIProviderManager>>().Object);
            
        // Setup default provider name to match agents AIServiceType ("OpenAI")
        defaultProviderMock.Setup(p => p.GetProviderName()).Returns("OpenAI");
        
        // Setup provider manager to return the default provider for "OpenAI"
        providerManagerMock
            .Setup(m => m.GetProviderByName("OpenAI"))
            .Returns(defaultProviderMock.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesProperties()
    {
        // Arrange
        var session = new Session(premise.Id, DateTime.UtcNow, premise, agents);
        
        // Act
        var runner = new SessionRunner(session, providerManagerMock.Object, loggerMock.Object);
        
        // Assert
        Assert.Same(session, runner.Session);
        Assert.Equal(premise.Id, runner.Session.Id);
        Assert.Equal(agents[0], runner.NextAgentToRespond);
        Assert.Empty(runner.GetConversationHistory());
    }
    
    [Fact]
    public void Constructor_WithNullSession_ThrowsArgumentNullException()
    {
        // Act & Assert
        Session? nullSession = null;
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new SessionRunner(nullSession!, providerManagerMock.Object, loggerMock.Object));
        
        Assert.Equal("session", exception.ParamName);
    }
    
    [Fact]
    public void Constructor_WithNullProviderManager_ThrowsArgumentNullException()
    {
        // Arrange
        var session = new Session(premise.Id, DateTime.UtcNow, premise, agents);
        
        // Act & Assert
        AIProviderManager? nullProviderManager = null;
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new SessionRunner(session, nullProviderManager!, loggerMock.Object));
        
        Assert.Equal("providerManager", exception.ParamName);
    }
    
    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var session = new Session(premise.Id, DateTime.UtcNow, premise, agents);
        
        // Act & Assert
        ILogger<SessionRunner>? nullLogger = null;
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new SessionRunner(session, providerManagerMock.Object, nullLogger!));
        
        Assert.Equal("logger", exception.ParamName);
    }
    
    [Fact]
    public async Task Next_FirstMessage_SendsPremiseContent()
    {
        // Arrange
        var session = new Session(premise.Id, DateTime.UtcNow, premise, agents);
        var runner = new SessionRunner(session, providerManagerMock.Object, loggerMock.Object);
        string expectedResponse = "This is a response from Agent1";
        
        defaultProviderMock.Setup(p => p.SendMessageAsync(
                agents[0],
                premise,
                It.IsAny<List<StormMessage>>()))
            .ReturnsAsync(expectedResponse);
        
        // Act
        await runner.Next();
        
        // Assert
        var messages = runner.GetConversationHistory();
        Assert.Single(messages);
        Assert.Equal(agents[0].Name, messages[0].AgentName);
        Assert.Equal(PromptTools.FormatMessageWithAgentNamePrefix(agents[0].Name, expectedResponse), messages[0].Content);
        
        // Verify the message was added to the session
        Assert.Single(runner.Session.Messages);
        Assert.Equal(messages[0], runner.Session.Messages[0]);
        
        // Verify the next agent has been rotated
        Assert.Equal(agents[1], runner.NextAgentToRespond);
        
        // Verify the AI provider was called with the correct parameters
        defaultProviderMock.Verify(p => p.SendMessageAsync(
            agents[0],
            premise,
            It.Is<List<StormMessage>>(m => m.Count == 0)),
            Times.Once);
    }
    
    [Fact]
    public async Task Next_SubsequentMessage_SendsPremiseAndHistory()
    {
        // Arrange
        var session = new Session(premise.Id, DateTime.UtcNow, premise, agents);
        var runner = new SessionRunner(session, providerManagerMock.Object, loggerMock.Object);
        
        // Add a user message to create some history
        runner.AddUserMessage("Hello agents");
        
        string expectedResponse = "This is a response from Agent1";
        
        defaultProviderMock.Setup(p => p.SendMessageAsync(
                agents[0],
                premise,
                It.IsAny<List<StormMessage>>()))
            .ReturnsAsync(expectedResponse);
        
        // Act
        await runner.Next();
        
        // Assert
        var messages = runner.GetConversationHistory();
        Assert.Equal(2, messages.Count);
        Assert.Equal(agents[0].Name, messages[1].AgentName);
        Assert.Equal(PromptTools.FormatMessageWithAgentNamePrefix(agents[0].Name, expectedResponse), messages[1].Content);
        
        // Verify the message was added to the session
        Assert.Equal(2, runner.Session.Messages.Count);
        Assert.Equal(messages[1], runner.Session.Messages[1]);
        
        // Verify the next agent has been rotated
        Assert.Equal(agents[1], runner.NextAgentToRespond);
        
        // Verify the AI provider was called with the correct parameters
        defaultProviderMock.Verify(p => p.SendMessageAsync(
            agents[0],
            premise,
            It.Is<List<StormMessage>>(m => m.Count == 1 && m[0].AgentName == "Human")),
            Times.Once);
    }
    
    [Fact]
    public async Task Next_WhenAIProviderFails_AddsErrorMessageAndRotatesAgent()
    {
        // Arrange
        var session = new Session(premise.Id, DateTime.UtcNow, premise, agents);
        var runner = new SessionRunner(session, providerManagerMock.Object, loggerMock.Object);
        var expectedException = new Exception("API failure");
        
        defaultProviderMock.Setup(p => p.SendMessageAsync(
                It.IsAny<Agent>(),
                It.IsAny<SessionPremise>(),
                It.IsAny<List<StormMessage>>()))
            .ThrowsAsync(expectedException);
        
        // Act
        await runner.Next();
        
        // Assert
        Assert.Single(runner.Session.Messages);
        var message = runner.Session.Messages[0];
        Assert.Equal(agents[0].Name, message.AgentName);
        Assert.Contains(">>>ERROR FETCHING RESPONSE<<<", message.Content);
        Assert.Contains(expectedException.Message, message.Content);
        Assert.Contains(expectedException.GetType().Name, message.Content);
        
        // Verify the agent was rotated
        Assert.Equal(agents[1], runner.NextAgentToRespond);
    }
    
    [Fact]
    public void AddUserMessage_AddsMessageToSession()
    {
        // Arrange
        var session = new Session(premise.Id, DateTime.UtcNow, premise, agents);
        var runner = new SessionRunner(session, providerManagerMock.Object, loggerMock.Object);
        string userContent = "This is a user message";
        string expectedFormattedContent = PromptTools.FormatMessageWithAgentNamePrefix("Human", userContent);
        
        // Act
        runner.AddUserMessage(userContent);
        
        // Assert
        var messages = runner.GetConversationHistory();
        Assert.Single(messages);
        Assert.Equal("Human", messages[0].AgentName);
        Assert.Equal(expectedFormattedContent, messages[0].Content);
        
        // Verify the message was added to the session
        Assert.Single(runner.Session.Messages);
        Assert.Equal(messages[0], runner.Session.Messages[0]);
        
        // Verify the next agent hasn't changed
        Assert.Equal(agents[0], runner.NextAgentToRespond);
    }
    
    [Fact]
    public async Task AgentRotation_FollowsConstructorOrder()
    {
        // Arrange
        var session = new Session(premise.Id, DateTime.UtcNow, premise, agents);
        var runner = new SessionRunner(session, providerManagerMock.Object, loggerMock.Object);
        
        // Setup AI provider to return responses with agent name to verify message assignment
        defaultProviderMock.Setup(p => p.SendMessageAsync(
                agents[0],
                premise,
                It.IsAny<List<StormMessage>>()))
            .ReturnsAsync("Response from Agent1");
            
        defaultProviderMock.Setup(p => p.SendMessageAsync(
                agents[1], 
                premise,
                It.IsAny<List<StormMessage>>()))
            .ReturnsAsync("Response from Agent2");
            
        defaultProviderMock.Setup(p => p.SendMessageAsync(
                agents[2],
                premise,
                It.IsAny<List<StormMessage>>()))
            .ReturnsAsync("Response from Agent3");
        
        // Assert initial state
        Assert.Equal(agents[0], runner.NextAgentToRespond);
        
        // First cycle of rotation
        await runner.Next(); // Agent1 responds, moves to Agent2
        await runner.Next(); // Agent2 responds, moves to Agent3
        await runner.Next(); // Agent3 responds, moves to Agent1
        
        // Verify we're back to the first agent
        Assert.Equal(agents[0], runner.NextAgentToRespond);
        
        // Verify messages were created correctly and in the right order
        var messages = runner.GetConversationHistory();
        Assert.Equal(3, messages.Count);
        
        Assert.Equal(agents[0].Name, messages[0].AgentName);
        Assert.Equal(PromptTools.FormatMessageWithAgentNamePrefix(agents[0].Name, "Response from Agent1"), messages[0].Content);
        
        Assert.Equal(agents[1].Name, messages[1].AgentName);
        Assert.Equal(PromptTools.FormatMessageWithAgentNamePrefix(agents[1].Name, "Response from Agent2"), messages[1].Content);
        
        Assert.Equal(agents[2].Name, messages[2].AgentName);
        Assert.Equal(PromptTools.FormatMessageWithAgentNamePrefix(agents[2].Name, "Response from Agent3"), messages[2].Content);
        
        // Second cycle to ensure the rotation continues correctly
        // Setup with correct parameter order for the next cycle
        defaultProviderMock.Setup(p => p.SendMessageAsync(
                agents[0],
                premise,
                It.IsAny<List<StormMessage>>()))
            .ReturnsAsync("Response from Agent1");
        
        await runner.Next(); // Agent1 responds again, moves to Agent2
        
        // Verify we're now at Agent2
        Assert.Equal(agents[1], runner.NextAgentToRespond);
        
        // Verify the new message was added
        messages = runner.GetConversationHistory();
        Assert.Equal(4, messages.Count);
        Assert.Equal(agents[0].Name, messages[3].AgentName);
        Assert.Equal(PromptTools.FormatMessageWithAgentNamePrefix(agents[0].Name, "Response from Agent1"), messages[3].Content);
    }
    
    [Fact]
    public void Constructor_WithExistingSession_DeterminesCorrectNextAgent()
    {
        // Arrange
        var existingSession = new Session("test-session", DateTime.UtcNow, premise, agents);
        
        // Add messages in sequence: Human → Agent1 → Agent2
        var humanMsg = new StormMessage("Human", DateTime.UtcNow.AddMinutes(-30), "[Human]: Hello agents");
        var agent1Msg = new StormMessage("Agent1", DateTime.UtcNow.AddMinutes(-25), "Response from Agent1");
        var agent2Msg = new StormMessage("Agent2", DateTime.UtcNow.AddMinutes(-20), "Response from Agent2");
        
        existingSession.AddMessage(humanMsg);
        existingSession.AddMessage(agent1Msg);
        existingSession.AddMessage(agent2Msg);
        
        // Act
        var runner = new SessionRunner(existingSession, providerManagerMock.Object, loggerMock.Object);
        
        // Assert
        // After Agent2 spoke, Agent3 should be next in rotation
        Assert.Equal(agents[2], runner.NextAgentToRespond);
        
        // Verify session is correctly set
        Assert.Same(existingSession, runner.Session);
        Assert.Equal(3, runner.GetConversationHistory().Count);
    }
    
    [Fact]
    public void Constructor_WithExistingSessionAndUnknownAgent_StartsWithFirstAgent()
    {
        // Arrange
        var existingSession = new Session("test-session", DateTime.UtcNow, premise, agents);
        
        // Add message from an unknown agent
        var humanMsg = new StormMessage("Human", DateTime.UtcNow.AddMinutes(-30), "[Human]: Hello agents");
        var unknownMsg = new StormMessage("UnknownAgent", DateTime.UtcNow.AddMinutes(-25), "Response from UnknownAgent");
        
        existingSession.AddMessage(humanMsg);
        existingSession.AddMessage(unknownMsg);
        
        // Act
        var runner = new SessionRunner(existingSession, providerManagerMock.Object, loggerMock.Object);
        
        // Assert
        // Should default to first agent since last agent isn't in our list
        Assert.Equal(agents[0], runner.NextAgentToRespond);
    }
    
    [Fact]
    public void Constructor_WithEmptyExistingSession_StartsWithFirstAgent()
    {
        // Arrange
        var emptySession = new Session("test-session", DateTime.UtcNow, premise, agents);
        
        // Act
        var runner = new SessionRunner(emptySession, providerManagerMock.Object, loggerMock.Object);
        
        // Assert
        Assert.Equal(agents[0], runner.NextAgentToRespond);
    }
    
    [Fact]
    public async Task Next_WithDifferentAIServiceTypeAgents_ShouldUseCorrectProvider()
    {
        // Arrange
        // Create mock providers
        var providerAMock = new Mock<IAIProvider>();
        var providerBMock = new Mock<IAIProvider>();
        
        // Set up the provider mocks to return distinct responses
        providerAMock
            .Setup(p => p.SendMessageAsync(It.IsAny<Agent>(), It.IsAny<SessionPremise>(), It.IsAny<List<StormMessage>>()))
            .ReturnsAsync("Response from Provider A");
        
        providerBMock
            .Setup(p => p.SendMessageAsync(It.IsAny<Agent>(), It.IsAny<SessionPremise>(), It.IsAny<List<StormMessage>>()))
            .ReturnsAsync("Response from Provider B");
        
        providerAMock
            .Setup(p => p.GetProviderName())
            .Returns("ProviderA");
            
        providerBMock
            .Setup(p => p.GetProviderName())
            .Returns("ProviderB");
        
        // Create a multi-provider manager with both providers
        var providerList = new List<IAIProvider> { providerAMock.Object, providerBMock.Object };
        var multiProviderManagerMock = new Mock<AIProviderManager>(
            providerList, 
            new Mock<ILogger<AIProviderManager>>().Object);
            
        // Setup provider manager to return the correct provider based on name
        multiProviderManagerMock
            .Setup(m => m.GetProviderByName("ProviderA"))
            .Returns(providerAMock.Object);
            
        multiProviderManagerMock
            .Setup(m => m.GetProviderByName("ProviderB"))
            .Returns(providerBMock.Object);
        
        // Create agents with different AIServiceTypes
        var agentA = new Agent("AgentA", "ProviderA", "model-a", "You are Agent A");
        var agentB = new Agent("AgentB", "ProviderB", "model-b", "You are Agent B");
        
        var agents = new List<Agent> { agentA, agentB };
        var premise = new SessionPremise("test-premise", "This is a test premise");
        var session = new Session(premise.Id, DateTime.UtcNow, premise, agents);
        
        // Set up the SessionRunner with the multi-provider manager
        var runner = new SessionRunner(session, multiProviderManagerMock.Object, loggerMock.Object);
        
        // Act
        await runner.Next(); // Agent A should use Provider A
        await runner.Next(); // Agent B should use Provider B
        
        // Assert
        // Provider A should be used only for Agent A
        providerAMock.Verify(p => p.SendMessageAsync(
            agentA, 
            premise,
            It.IsAny<List<StormMessage>>()),
            Times.Once, 
            "Provider A should be used exactly once for Agent A");
        
        // Provider A should NOT be used for Agent B
        providerAMock.Verify(p => p.SendMessageAsync(
            agentB,
            premise,
            It.IsAny<List<StormMessage>>()),
            Times.Never,
            "Provider A should never be used for Agent B");
        
        // Provider B should be used only for Agent B
        providerBMock.Verify(p => p.SendMessageAsync(
            agentB,
            premise,
            It.IsAny<List<StormMessage>>()),
            Times.Once,
            "Provider B should be used exactly once for Agent B");
    }
}
