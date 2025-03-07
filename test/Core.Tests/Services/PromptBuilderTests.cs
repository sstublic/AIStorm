using AIStorm.Core.Models;
using AIStorm.Core.SessionManagement;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace AIStorm.Core.Tests.Services
{
    public class PromptBuilderTests
    {
        private readonly Mock<ILogger<PromptBuilder>> loggerMock;
        private readonly PromptBuilder promptBuilder;
        private readonly Agent agent;
        private readonly SessionPremise premise;

        public PromptBuilderTests()
        {
            // Set up test components
            loggerMock = new Mock<ILogger<PromptBuilder>>();
            promptBuilder = new PromptBuilder(loggerMock.Object);
            
            // Create test data
            agent = new Agent("Test Agent", "OpenAI", "gpt-4o", "You are a test agent with specific behavior.");
            premise = new SessionPremise("test-premise", "This is a test premise for a brainstorming session.");
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Arrange & Act
            var builder = new PromptBuilder(loggerMock.Object);
            
            // Assert
            Assert.NotNull(builder);
        }


        [Fact]
        public void BuildPrompt_WithEmptyHistory_ReturnsSystemAndUserMessage()
        {
            // Arrange
            var history = new List<StormMessage>();
            
            // Act
            var result = promptBuilder.BuildPrompt(agent, premise, history);
            
            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal("system", result[0].Role);
            Assert.Equal("user", result[1].Role);
            
            // Verify system message contains extended prompt
            string expectedSystemPrompt = PromptTools.CreateExtendedSystemPrompt(agent, premise);
            Assert.Equal(expectedSystemPrompt, result[0].Content);
            Assert.Equal(expectedSystemPrompt, result[1].Content);
        }
        
        [Fact]
        public void BuildPrompt_AlwaysIncludesUserMessageWithSystemContent()
        {
            // This test is redundant with BuildPrompt_WithEmptyHistory_ReturnsSystemAndUserMessage
            // But we're keeping it to maintain consistency with the test suite structure
            // Arrange
            var history = new List<StormMessage>();
            
            // Act
            var result = promptBuilder.BuildPrompt(agent, premise, history);
            
            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal("system", result[0].Role);
            Assert.Equal("user", result[1].Role);
            Assert.Equal(result[0].Content, result[1].Content);
        }
        
        [Fact]
        public void BuildPrompt_WithSingleUserMessage_ReturnsSystemAndUserMessage()
        {
            // Arrange
            var history = new List<StormMessage>
            {
                new StormMessage("Human", DateTime.UtcNow, "This is a test message from the user.")
            };
            
            // Act
            var result = promptBuilder.BuildPrompt(agent, premise, history);
            
            // Assert
            Assert.Equal(3, result.Length);
            
            // Verify first message is system prompt
            Assert.Equal("system", result[0].Role);
            
            // Verify second message is user message with system content
            Assert.Equal("user", result[1].Role);
            Assert.Equal(result[0].Content, result[1].Content);
            
            // Verify third message is the user message from history
            Assert.Equal("user", result[2].Role);
            Assert.Equal(history[0].Content, result[2].Content);
        }
        
        [Fact]
        public void BuildPrompt_WithMessagesFromAgent_AssignsCorrectRoles()
        {
            // Arrange
            var history = new List<StormMessage>
            {
                new StormMessage("Human", DateTime.UtcNow.AddMinutes(-2), "What do you think?"),
                new StormMessage(agent.Name, DateTime.UtcNow.AddMinutes(-1), "I think it's great."),
                new StormMessage("Human", DateTime.UtcNow, "Can you elaborate?")
            };
            
            // Act
            var result = promptBuilder.BuildPrompt(agent, premise, history);
            
            // Assert
            Assert.Equal(5, result.Length);
            
            // Verify system message
            Assert.Equal("system", result[0].Role);
            
            // Verify second message is user message with system content
            Assert.Equal("user", result[1].Role);
            Assert.Equal(result[0].Content, result[1].Content);
            
            // Verify roles are assigned correctly for history messages
            Assert.Equal("user", result[2].Role); // Human
            Assert.Equal("assistant", result[3].Role); // Test Agent (matching current agent)
            Assert.Equal("user", result[4].Role); // Human
            
            // Verify content is preserved
            Assert.Equal(history[0].Content, result[2].Content);
            Assert.Equal(history[1].Content, result[3].Content);
            Assert.Equal(history[2].Content, result[4].Content);
        }
        
        [Fact]
        public void BuildPrompt_WithMessagesFromMultipleAgents_AssignsCorrectRoles()
        {
            // Arrange
            var history = new List<StormMessage>
            {
                new StormMessage("Human", DateTime.UtcNow.AddMinutes(-3), "What are your thoughts?"),
                new StormMessage(agent.Name, DateTime.UtcNow.AddMinutes(-2), "Approach A is best."),
                new StormMessage("Other Agent", DateTime.UtcNow.AddMinutes(-1), "I prefer approach B."),
                new StormMessage("Third Agent", DateTime.UtcNow, "I suggest approach C.")
            };
            
            // Act
            var result = promptBuilder.BuildPrompt(agent, premise, history);
            
            // Assert
            Assert.Equal(6, result.Length);
            
            // Verify system message
            Assert.Equal("system", result[0].Role);
            
            // Verify second message is user message with system content
            Assert.Equal("user", result[1].Role);
            Assert.Equal(result[0].Content, result[1].Content);
            
            // Verify roles are assigned correctly
            Assert.Equal("user", result[2].Role); // Human
            Assert.Equal("assistant", result[3].Role); // Test Agent (matching current agent)
            Assert.Equal("user", result[4].Role); // Other Agent
            Assert.Equal("user", result[5].Role); // Third Agent
            
            // Verify content is preserved
            Assert.Equal(history[0].Content, result[2].Content);
            Assert.Equal(history[1].Content, result[3].Content);
            Assert.Equal(history[2].Content, result[4].Content);
            Assert.Equal(history[3].Content, result[5].Content);
        }
        
        [Fact]
        public void BuildPrompt_WithLongConversationHistory_MaintainsCorrectOrder()
        {
            // Arrange
            var history = new List<StormMessage>();
            
            // Create a conversation with 10 alternating messages
            for (int i = 0; i < 5; i++)
            {
                history.Add(new StormMessage("Human", DateTime.UtcNow.AddMinutes(-10 + i*2), $"Human message {i+1}"));
                history.Add(new StormMessage(agent.Name, DateTime.UtcNow.AddMinutes(-9 + i*2), $"Agent message {i+1}"));
            }
            
            // Act
            var result = promptBuilder.BuildPrompt(agent, premise, history);
            
            // Assert
            Assert.Equal(12, result.Length); // 10 messages + system + initial user message
            
            // Verify system message and initial user message
            Assert.Equal("system", result[0].Role);
            Assert.Equal("user", result[1].Role);
            Assert.Equal(result[0].Content, result[1].Content);
            
            // Verify message order and roles
            for (int i = 0; i < 5; i++)
            {
                // Human messages should have "user" role
                Assert.Equal("user", result[i*2 + 2].Role);
                Assert.Equal($"Human message {i+1}", result[i*2 + 2].Content);
                
                // Agent messages should have "assistant" role
                Assert.Equal("assistant", result[i*2 + 3].Role);
                Assert.Equal($"Agent message {i+1}", result[i*2 + 3].Content);
            }
        }
        
        [Fact]
        public void BuildPrompt_WithSpecialCharactersInMessages_PreservesContent()
        {
            // Arrange
            string specialMessage = "Message with special chars: !@#$%^&*()_+{}|:<>?~`-=[]\\;',./\nand newlines\ttabs";
            var history = new List<StormMessage>
            {
                new StormMessage("Human", DateTime.UtcNow, specialMessage)
            };
            
            // Act
            var result = promptBuilder.BuildPrompt(agent, premise, history);
            
            // Assert
            Assert.Equal(3, result.Length);
            Assert.Equal("user", result[2].Role);
            Assert.Equal(specialMessage, result[2].Content);
        }
        
        [Fact]
        public void BuildPrompt_WithNoAgentMatchingHistory_AssignsAllMessagesToUser()
        {
            // Arrange
            var differentAgent = new Agent("Different Agent", "OpenAI", "gpt-4o", "You are a different agent.");
            var history = new List<StormMessage>
            {
                new StormMessage("Human", DateTime.UtcNow.AddMinutes(-2), "Hello!"),
                new StormMessage("Some Agent", DateTime.UtcNow.AddMinutes(-1), "Hi there!"),
                new StormMessage("Another Agent", DateTime.UtcNow, "Hello as well!")
            };
            
            // Act
            var result = promptBuilder.BuildPrompt(differentAgent, premise, history);
            
            // Assert
            Assert.Equal(5, result.Length);
            
            // Verify system message and initial user message
            Assert.Equal("system", result[0].Role);
            Assert.Equal("user", result[1].Role);
            Assert.Equal(result[0].Content, result[1].Content);
            
            // Verify all history messages are assigned "user" role
            Assert.Equal("user", result[2].Role);
            Assert.Equal("user", result[3].Role);
            Assert.Equal("user", result[4].Role);
            
            // Verify content is preserved
            Assert.Equal(history[0].Content, result[2].Content);
            Assert.Equal(history[1].Content, result[3].Content);
            Assert.Equal(history[2].Content, result[4].Content);
        }
        
        [Fact]
        public void BuildPrompt_WithMixedCaseAgentName_AssignsRolesCorrectly()
        {
            // Arrange - Create agent with mixed case name
            var mixedCaseAgent = new Agent("MixedCase", "OpenAI", "gpt-4o", "You are an agent with a mixed case name.");
            
            // Create history with the same name but different casing
            var history = new List<StormMessage>
            {
                new StormMessage("Human", DateTime.UtcNow.AddMinutes(-2), "Hello!"),
                new StormMessage("MIXEDCASE", DateTime.UtcNow.AddMinutes(-1), "Hi there!"),
                new StormMessage("mixedcase", DateTime.UtcNow, "Hello again!")
            };
            
            // Act
            var result = promptBuilder.BuildPrompt(mixedCaseAgent, premise, history);
            
            // Assert
            Assert.Equal(5, result.Length);
            
            // Verify system message and initial user message
            Assert.Equal("system", result[0].Role);
            Assert.Equal("user", result[1].Role);
            Assert.Equal(result[0].Content, result[1].Content);
            
            // Verify roles - case-sensitive comparison should make all agent messages "user"
            Assert.Equal("user", result[2].Role); // Human
            Assert.Equal("user", result[3].Role); // MIXEDCASE - different case from MixedCase
            Assert.Equal("user", result[4].Role); // mixedcase - different case from MixedCase
            
            // Verify content is preserved
            Assert.Equal(history[0].Content, result[2].Content);
            Assert.Equal(history[1].Content, result[3].Content);
            Assert.Equal(history[2].Content, result[4].Content);
        }
    }
}
