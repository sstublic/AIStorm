namespace AIStorm.Core.Services;

using AIStorm.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class SessionRunner
{
    private readonly List<Agent> agents;
    private readonly SessionPremise premise;
    private readonly IAIProvider aiProvider;
    private readonly ILogger<SessionRunner> logger;
    private Session session;
    private int currentAgentIndex = 0;

    public SessionRunner(List<Agent> agents, SessionPremise premise, IAIProvider aiProvider, ILogger<SessionRunner> logger)
    {
        this.agents = agents ?? throw new ArgumentNullException(nameof(agents));
        
        if (agents.Count == 0)
        {
            throw new ArgumentException("At least one agent must be provided", nameof(agents));
        }
        
        this.premise = premise ?? throw new ArgumentNullException(nameof(premise));
        this.aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        session = new Session(
            id: premise.Id,
            created: DateTime.UtcNow,
            description: $"Session based on premise: {premise.Id}"
        );
    }

    public Session Session => session;

    public Agent NextAgentToRespond => agents[currentAgentIndex];

    public async Task Next()
    {
        Agent agent = NextAgentToRespond;
        
        logger.LogInformation("Getting response from next agent: {AgentName}", agent.Name);
        
        List<StormMessage> conversationHistory = GetConversationHistory();
        
        string userMessage = conversationHistory.Count == 0 
            ? premise.Content 
            : $"Premise: {premise.Content}\n\nContinue the conversation based on the above premise and the conversation history.";
            
        string response = await aiProvider.SendMessageAsync(agent, conversationHistory, userMessage);
        
        var message = new StormMessage(agent.Name, DateTime.UtcNow, response);
        
        session.Messages.Add(message);
        
        MoveToNextAgent();
    }

    public List<StormMessage> GetConversationHistory()
    {
        return session.Messages.ToList();
    }

    public void AddUserMessage(string content)
    {
        var message = new StormMessage("Human", DateTime.UtcNow, content);
        session.Messages.Add(message);
    }

    private void MoveToNextAgent()
    {
        // Advance to the next agent in the rotation, wrapping around to the first agent after the last one
        currentAgentIndex = (currentAgentIndex + 1) % agents.Count;
    }
}
