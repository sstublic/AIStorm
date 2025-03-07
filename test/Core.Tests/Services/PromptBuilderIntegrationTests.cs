using AIStorm.Core.Models;
using AIStorm.Core.SessionManagement;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace AIStorm.Core.Tests.Services
{
    public class PromptBuilderIntegrationTests
    {
        private readonly Mock<ILogger<PromptBuilder>> loggerMock;
        private readonly PromptBuilder promptBuilder;
        private readonly string testDataPath;

        public PromptBuilderIntegrationTests()
        {
            loggerMock = new Mock<ILogger<PromptBuilder>>();
            promptBuilder = new PromptBuilder(loggerMock.Object);
            
            // Path to test data
            testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "TestData");
        }

        [Fact]
        public void BuildPrompt_UsingAgentTemplates_CreatesExpectedPrompt()
        {
            // Arrange
            // Create agents based on the template structures
            var creativeAgent = new Agent(
                "Creative Thinker", 
                "OpenAI", 
                "gpt-4o", 
                "You are a creative thinking expert who specializes in generating innovative ideas."
            );
            
            var criticalAgent = new Agent(
                "Critical Analyst", 
                "OpenAI", 
                "gpt-4o", 
                "You are a critical analyst who evaluates ideas rigorously."
            );
            
            var premise = new SessionPremise(
                "test-premise", 
                "This is a test premise for a brainstorming session."
            );
            
            // Create minimal conversation history
            var messages = new List<StormMessage>
            {
                new StormMessage("user", DateTime.Parse("2025-03-01T15:01:00"), "What do you think about this?"),
                new StormMessage("Creative Thinker", DateTime.Parse("2025-03-01T15:01:30"), "I have several creative ideas."),
                new StormMessage("Critical Analyst", DateTime.Parse("2025-03-01T15:02:00"), "Let me analyze these ideas.")
            };

            // Act - build prompt for creative agent
            var result = promptBuilder.BuildPrompt(creativeAgent, premise, messages);

            // Assert
            Assert.Equal(5, result.Length);
            
            // Verify system message contains agent name
            Assert.Equal("system", result[0].Role);
            Assert.Contains("You are Creative Thinker.", result[0].Content);
            
            // Verify second message is user message with system content
            Assert.Equal("user", result[1].Role);
            Assert.Equal(result[0].Content, result[1].Content);
            
            // Verify roles assigned correctly (creative agent's perspective)
            Assert.Equal("user", result[2].Role); // user message
            Assert.Equal("assistant", result[3].Role); // Creative Thinker message (current agent)
            Assert.Equal("user", result[4].Role); // Critical Analyst message (different agent)
            
            // Act - build prompt for critical agent
            result = promptBuilder.BuildPrompt(criticalAgent, premise, messages);
            
            // Assert
            Assert.Equal(5, result.Length);
            
            // Verify system message contains agent name
            Assert.Equal("system", result[0].Role);
            Assert.Contains("You are Critical Analyst.", result[0].Content);
            
            // Verify second message is user message with system content
            Assert.Equal("user", result[1].Role);
            Assert.Equal(result[0].Content, result[1].Content);
            
            // Verify roles assigned correctly (critical agent's perspective)
            Assert.Equal("user", result[2].Role); // user message
            Assert.Equal("user", result[3].Role); // Creative Thinker message (different agent)
            Assert.Equal("assistant", result[4].Role); // Critical Analyst message (current agent)
        }
        
        [Fact]
        public void BuildPrompt_WithMultipleAgentsAndSession_MaintainsCorrectRoles()
        {
            // Arrange
            var agents = new List<Agent> {
                new Agent("Creative Thinker", "OpenAI", "gpt-4o", "You are a creative thinker."),
                new Agent("Critical Analyst", "OpenAI", "gpt-4o", "You are a critical analyst."),
                new Agent("Practical Implementer", "OpenAI", "gpt-4o", "You are a practical implementer.")
            };
            
            var premise = new SessionPremise("test-premise", "Test premise content");
            
            // Create a conversation with all agents participating
            var messages = new List<StormMessage>();
            
            // Add a user message
            messages.Add(new StormMessage("user", DateTime.UtcNow.AddMinutes(-5), "What are your thoughts?"));
            
            // Add a message from each agent
            foreach (var agent in agents)
            {
                messages.Add(new StormMessage(
                    agent.Name, 
                    DateTime.UtcNow.AddMinutes(-4 + messages.Count), 
                    $"Response from {agent.Name}"));
            }
            
            // Act - Test building prompt for each agent
            foreach (var currentAgent in agents)
            {
                var result = promptBuilder.BuildPrompt(currentAgent, premise, messages);
                
                // Assert
                Assert.Equal(messages.Count + 2, result.Length); // +1 for system message, +1 for initial user message
                
                Assert.Equal("system", result[0].Role);
                Assert.Contains($"You are {currentAgent.Name}.", result[0].Content);
                
                // Verify second message is user message with system content
                Assert.Equal("user", result[1].Role);
                Assert.Equal(result[0].Content, result[1].Content);
                
                // Check each message role
                for (int i = 0; i < messages.Count; i++)
                {
                    string expectedRole = messages[i].AgentName == currentAgent.Name ? "assistant" : "user";
                    Assert.Equal(expectedRole, result[i + 2].Role);
                }
            }
        }
        
        [Fact]
        public void BuildPrompt_WithPrefixedMessages_PreservesFormattedContent()
        {
            // Arrange
            var agent = new Agent("Test Agent", "OpenAI", "gpt-4o", "You are a test agent.");
            var premise = new SessionPremise("test-premise", "Test premise");
            
            // Create messages with markdown prefixes
            var messages = new List<StormMessage>
            {
                new StormMessage("user", DateTime.UtcNow, "## [user]:\n\nWhat do you think?"),
                new StormMessage("Test Agent", DateTime.UtcNow, "## [Test Agent]:\n\nThis is my response.")
            };
            
            // Act
            var result = promptBuilder.BuildPrompt(agent, premise, messages);
            
            // Assert
            Assert.Equal(4, result.Length);
            
            // Verify system message and initial user message
            Assert.Equal("system", result[0].Role);
            Assert.Equal("user", result[1].Role);
            Assert.Equal(result[0].Content, result[1].Content);
            
            // Verify message content is preserved with prefixes
            Assert.Equal(messages[0].Content, result[2].Content);
            Assert.Equal(messages[1].Content, result[3].Content);
        }
    }
}
