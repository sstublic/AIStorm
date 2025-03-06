namespace AIStorm.Core.SessionManagement;

using AIStorm.Core.Models;
using AIStorm.Core.SessionManagement;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

public class PromptBuilder : IPromptBuilder
{
    private readonly ILogger<PromptBuilder> logger;
    
    public PromptBuilder(ILogger<PromptBuilder> logger)
    {
        this.logger = logger;
    }
    
    public PromptMessage[] BuildPrompt(Agent agent, SessionPremise premise, List<StormMessage> history)
    {
        logger.LogDebug("Building prompt for agent {AgentName} with premise and {MessageCount} history messages", 
            agent.Name, history.Count);
            
        string systemPrompt = PromptTools.CreateExtendedSystemPrompt(agent, premise);
        
        var messages = new List<PromptMessage>
        {
            // Always add system message
            new PromptMessage("system", systemPrompt),
            
            // Always add first user message with same content as system message
            // This ensures the conversation always has at least one user message
            new PromptMessage("user", systemPrompt)
        };
        
        // Add conversation history
        foreach (var message in history)
        {
            string role = message.AgentName == agent.Name ? "assistant" : "user";
            messages.Add(new PromptMessage(role, message.Content));
        }
        
        logger.LogDebug("Built prompt with {MessageCount} messages", messages.Count);
        return messages.ToArray();
    }
}
