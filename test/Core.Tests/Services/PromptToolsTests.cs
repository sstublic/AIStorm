using AIStorm.Core.Models;
using AIStorm.Core.SessionManagement;
using System;
using Xunit;

namespace AIStorm.Core.Tests.Services
{
    public class PromptToolsTests
    {
        [Fact]
        public void RemoveAgentNamePrefixFromMessage_WithPrefixPresent_RemovesPrefix()
        {
            // Arrange
            string message = "[Agent Name]: This is the message content";
            
            // Act
            var result = PromptTools.RemoveAgentNamePrefixFromMessage(message);
            
            // Assert
            Assert.Equal("This is the message content", result);
        }
        
        [Fact]
        public void RemoveAgentNamePrefixFromMessage_WithMarkdownPrefix_RemovesPrefix()
        {
            // Arrange
            string message = "## [Agent Name]:\n\nThis is the message content";
            
            // Act
            var result = PromptTools.RemoveAgentNamePrefixFromMessage(message);
            
            // Assert
            Assert.Equal("This is the message content", result);
        }
        
        [Fact]
        public void RemoveAgentNamePrefixFromMessage_WithMultiplePrefixes_RemovesAllPrefixes()
        {
            // Arrange
            string message = "[Agent1]: [Agent2]: This is the message content";
            
            // Act
            var result = PromptTools.RemoveAgentNamePrefixFromMessage(message);
            
            // Assert
            Assert.Equal("This is the message content", result);
        }
        
        [Fact]
        public void RemoveAgentNamePrefixFromMessage_WithoutPrefix_ReturnsOriginalMessage()
        {
            // Arrange
            string message = "This is a message without a prefix";
            
            // Act
            var result = PromptTools.RemoveAgentNamePrefixFromMessage(message);
            
            // Assert
            Assert.Equal(message, result);
        }
        
        [Fact]
        public void RemoveAgentNamePrefixFromMessage_WithNullParameter_ThrowsArgumentNullException()
        {
            // Arrange
            string? nullString = null;
            
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => PromptTools.RemoveAgentNamePrefixFromMessage(nullString!));
        }
        
        [Fact]
        public void RemoveAgentNamePrefixFromMessage_WithEmptyString_ReturnsEmptyString()
        {
            // Arrange & Act & Assert
            Assert.Equal(string.Empty, PromptTools.RemoveAgentNamePrefixFromMessage(string.Empty));
        }
        
        [Fact]
        public void FormatMessageWithAgentNamePrefix_AddsCorrectPrefix()
        {
            // Arrange
            string agentName = "TestAgent";
            string content = "This is the message content";
            
            // Act
            string result = PromptTools.FormatMessageWithAgentNamePrefix(agentName, content);
            
            // Assert
            Assert.Equal("## [TestAgent]:\n\nThis is the message content", result);
        }
        
        [Fact]
        public void FormatMessageWithAgentNamePrefix_WithEmptyContent_AddsOnlyPrefix()
        {
            // Arrange
            string agentName = "TestAgent";
            string content = "";
            
            // Act
            string result = PromptTools.FormatMessageWithAgentNamePrefix(agentName, content);
            
            // Assert
            Assert.Equal("## [TestAgent]:\n\n", result);
        }
        
        [Fact]
        public void CreateExtendedSystemPrompt_ContainsAgentNameAndPrompt()
        {
            // Arrange
            var agent = new Agent("Test Agent", "OpenAI", "gpt-4", "You are a test agent with specific behavior.");
            var premise = new SessionPremise("test-premise", "This is a test premise.");
            
            // Act
            string result = PromptTools.CreateExtendedSystemPrompt(agent, premise);
            
            // Assert
            Assert.Contains("You are Test Agent.", result);
            Assert.Contains("You are a test agent with specific behavior.", result);
        }
        
        [Fact]
        public void CreateExtendedSystemPrompt_ContainsPremiseContent()
        {
            // Arrange
            var agent = new Agent("Test Agent", "OpenAI", "gpt-4", "You are a test agent.");
            var premise = new SessionPremise("test-premise", "This is a test premise with specific instructions.");
            
            // Act
            string result = PromptTools.CreateExtendedSystemPrompt(agent, premise);
            
            // Assert
            Assert.Contains("## Context of the conversation\nThis is a test premise with specific instructions.", result);
        }
        
        [Fact]
        public void CreateExtendedSystemPrompt_ContainsPrefixInstructions()
        {
            // Arrange
            var agent = new Agent("Test Agent", "OpenAI", "gpt-4", "You are a test agent.");
            var premise = new SessionPremise("test-premise", "This is a test premise.");
            
            // Act
            string result = PromptTools.CreateExtendedSystemPrompt(agent, premise);
            
            // Assert
            Assert.Contains("When responding, DO NOT add the prefix to your response!", result);
            Assert.Contains("You will be provided with the history of the conversation", result);
        }
    }
}
