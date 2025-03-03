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
    private readonly Mock<IAIProvider> aiProviderMock;
    private readonly Mock<ILogger<SessionRunner>> loggerMock;

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
        
        aiProviderMock = new Mock<IAIProvider>();
        loggerMock = new Mock<ILogger<SessionRunner>>();
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesProperties()
    {
        // Act
        var runner = new SessionRunner(agents, premise, aiProviderMock.Object, loggerMock.Object);
        
        // Assert
        Assert.NotNull(runner.Session);
        Assert.Equal(premise.Id, runner.Session.Id);
        Assert.Equal(agents[0], runner.NextAgentToRespond);
        Assert.Empty(runner.GetConversationHistory());
    }
    
    [Fact]
    public void Constructor_WithNullAgents_ThrowsArgumentNullException()
    {
        // Act & Assert
        List<Agent>? nullAgents = null;
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new SessionRunner(nullAgents!, premise, aiProviderMock.Object, loggerMock.Object));
        
        Assert.Equal("agents", exception.ParamName);
    }
    
    [Fact]
    public void Constructor_WithEmptyAgents_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new SessionRunner(new List<Agent>(), premise, aiProviderMock.Object, loggerMock.Object));
        
        Assert.Equal("agents", exception.ParamName);
    }
    
    [Fact]
    public void Constructor_WithNullPremise_ThrowsArgumentNullException()
    {
        // Act & Assert
        SessionPremise? nullPremise = null;
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new SessionRunner(agents, nullPremise!, aiProviderMock.Object, loggerMock.Object));
        
        Assert.Equal("premise", exception.ParamName);
    }
    
    [Fact]
    public void Constructor_WithNullAIProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        IAIProvider? nullProvider = null;
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new SessionRunner(agents, premise, nullProvider!, loggerMock.Object));
        
        Assert.Equal("aiProvider", exception.ParamName);
    }
    
    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        ILogger<SessionRunner>? nullLogger = null;
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new SessionRunner(agents, premise, aiProviderMock.Object, nullLogger!));
        
        Assert.Equal("logger", exception.ParamName);
    }
    
    [Fact]
    public async Task Next_FirstMessage_SendsPremiseContent()
    {
        // Arrange
        var runner = new SessionRunner(agents, premise, aiProviderMock.Object, loggerMock.Object);
        string expectedResponse = "This is a response from Agent1";
        
        aiProviderMock.Setup(p => p.SendMessageAsync(
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
        Assert.Equal(expectedResponse, messages[0].Content);
        
        // Verify the message was added to the session
        Assert.Single(runner.Session.Messages);
        Assert.Equal(messages[0], runner.Session.Messages[0]);
        
        // Verify the next agent has been rotated
        Assert.Equal(agents[1], runner.NextAgentToRespond);
        
        // Verify the AI provider was called with the correct parameters
        aiProviderMock.Verify(p => p.SendMessageAsync(
            agents[0],
            premise,
            It.Is<List<StormMessage>>(m => m.Count == 0)),
            Times.Once);
    }
    
    [Fact]
    public async Task Next_SubsequentMessage_SendsPremiseAndHistory()
    {
        // Arrange
        var runner = new SessionRunner(agents, premise, aiProviderMock.Object, loggerMock.Object);
        
        // Add a user message to create some history
        runner.AddUserMessage("Hello agents");
        
        string expectedResponse = "This is a response from Agent1";
        
        aiProviderMock.Setup(p => p.SendMessageAsync(
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
        Assert.Equal(expectedResponse, messages[1].Content);
        
        // Verify the message was added to the session
        Assert.Equal(2, runner.Session.Messages.Count);
        Assert.Equal(messages[1], runner.Session.Messages[1]);
        
        // Verify the next agent has been rotated
        Assert.Equal(agents[1], runner.NextAgentToRespond);
        
        // Verify the AI provider was called with the correct parameters
        aiProviderMock.Verify(p => p.SendMessageAsync(
            agents[0],
            premise,
            It.Is<List<StormMessage>>(m => m.Count == 1 && m[0].AgentName == "Human")),
            Times.Once);
    }
    
    [Fact]
    public async Task Next_WhenAIProviderFails_PropagatesException()
    {
        // Arrange
        var runner = new SessionRunner(agents, premise, aiProviderMock.Object, loggerMock.Object);
        var expectedException = new Exception("API failure");
        
        aiProviderMock.Setup(p => p.SendMessageAsync(
                It.IsAny<Agent>(),
                It.IsAny<SessionPremise>(),
                It.IsAny<List<StormMessage>>()))
            .ThrowsAsync(expectedException);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => runner.Next());
        Assert.Same(expectedException, exception);
        
        // Verify no messages were added
        Assert.Empty(runner.Session.Messages);
        
        // Verify the agent wasn't rotated
        Assert.Equal(agents[0], runner.NextAgentToRespond);
    }
    
    [Fact]
    public void AddUserMessage_AddsMessageToSession()
    {
        // Arrange
        var runner = new SessionRunner(agents, premise, aiProviderMock.Object, loggerMock.Object);
        string userContent = "This is a user message";
        string expectedFormattedContent = $"[Human]: {userContent}";
        
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
        var runner = new SessionRunner(agents, premise, aiProviderMock.Object, loggerMock.Object);
        
        // Setup AI provider to return responses with agent name to verify message assignment
        aiProviderMock.Setup(p => p.SendMessageAsync(
                agents[0],
                premise,
                It.IsAny<List<StormMessage>>()))
            .ReturnsAsync("Response from Agent1");
            
        aiProviderMock.Setup(p => p.SendMessageAsync(
                agents[1], 
                premise,
                It.IsAny<List<StormMessage>>()))
            .ReturnsAsync("Response from Agent2");
            
        aiProviderMock.Setup(p => p.SendMessageAsync(
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
        Assert.Equal("Response from Agent1", messages[0].Content);
        
        Assert.Equal(agents[1].Name, messages[1].AgentName);
        Assert.Equal("Response from Agent2", messages[1].Content);
        
        Assert.Equal(agents[2].Name, messages[2].AgentName);
        Assert.Equal("Response from Agent3", messages[2].Content);
        
        // Second cycle to ensure the rotation continues correctly
        // Setup with correct parameter order for the next cycle
        aiProviderMock.Setup(p => p.SendMessageAsync(
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
        Assert.Equal("Response from Agent1", messages[3].Content);
    }
    
    [Fact]
    public void Constructor_WithExistingSession_DeterminesCorrectNextAgent()
    {
        // Arrange
        var existingSession = new Session("test-session", DateTime.UtcNow, "Test Session", premise);
        
        // Add messages in sequence: Human → Agent1 → Agent2
        existingSession.Messages.Add(new StormMessage("Human", DateTime.UtcNow.AddMinutes(-30), "[Human]: Hello agents"));
        existingSession.Messages.Add(new StormMessage("Agent1", DateTime.UtcNow.AddMinutes(-25), "Response from Agent1"));
        existingSession.Messages.Add(new StormMessage("Agent2", DateTime.UtcNow.AddMinutes(-20), "Response from Agent2"));
        
        // Act
        var runner = new SessionRunner(agents, premise, aiProviderMock.Object, loggerMock.Object, existingSession);
        
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
        var existingSession = new Session("test-session", DateTime.UtcNow, "Test Session", premise);
        
        // Add message from an unknown agent
        existingSession.Messages.Add(new StormMessage("Human", DateTime.UtcNow.AddMinutes(-30), "[Human]: Hello agents"));
        existingSession.Messages.Add(new StormMessage("UnknownAgent", DateTime.UtcNow.AddMinutes(-25), "Response from UnknownAgent"));
        
        // Act
        var runner = new SessionRunner(agents, premise, aiProviderMock.Object, loggerMock.Object, existingSession);
        
        // Assert
        // Should default to first agent since last agent isn't in our list
        Assert.Equal(agents[0], runner.NextAgentToRespond);
    }
    
    [Fact]
    public void Constructor_WithEmptyExistingSession_StartsWithFirstAgent()
    {
        // Arrange
        var emptySession = new Session("test-session", DateTime.UtcNow, "Empty Test Session", premise);
        
        // Act
        var runner = new SessionRunner(agents, premise, aiProviderMock.Object, loggerMock.Object, emptySession);
        
        // Assert
        Assert.Equal(agents[0], runner.NextAgentToRespond);
    }
}
