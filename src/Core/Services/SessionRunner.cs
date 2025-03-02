namespace AIStorm.Core.Services;

using AIStorm.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class SessionRunner
{
    private readonly IAIProvider aiProvider;
    private readonly ILogger<SessionRunner> logger;
    private Session session;
    private int currentAgentIndex = 0;

    public SessionRunner(List<Agent> agents, SessionPremise premise, IAIProvider aiProvider, ILogger<SessionRunner> logger, Session? existingSession = null)
    {
        agents = agents ?? throw new ArgumentNullException(nameof(agents));
        
        if (agents.Count == 0)
        {
            throw new ArgumentException("At least one agent must be provided", nameof(agents));
        }
        
        premise = premise ?? throw new ArgumentNullException(nameof(premise));
        this.aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        if (existingSession != null)
        {
            this.session = existingSession;
            // Determine which agent should be next in the rotation
            List<Agent> agentsToUse = existingSession.Agents.Count > 0 ? existingSession.Agents : agents;
            this.currentAgentIndex = GetNextAgentIndexFromHistory(session.Messages, agentsToUse);
            logger.LogInformation("Initialized SessionRunner with existing session: {SessionId}", session.Id);
        }
        else
        {
            this.session = new Session(
                id: premise.Id,
                created: DateTime.UtcNow,
                description: $"Session based on premise: {premise.Id}",
                premise: premise
            );
            
            // Copy agents to session
            foreach (var agent in agents)
            {
                this.session.Agents.Add(agent);
            }
            
            logger.LogInformation("Initialized new SessionRunner with premise: {PremiseId}", premise.Id);
        }
    }

    public Session Session => session;

    public Agent NextAgentToRespond => session.Agents[currentAgentIndex];

    public async Task Next()
    {
        Agent agent = NextAgentToRespond;
        
        logger.LogInformation("Getting response from next agent: {AgentName}", agent.Name);
        
        List<StormMessage> conversationHistory = GetConversationHistory();
        
        string userMessage = conversationHistory.Count == 0 
            ? session.Premise.Content 
            : $"Premise: {session.Premise.Content}\n\nContinue the conversation based on the above premise and the conversation history.";
            
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
        // Add a Human message with the [Human]: prefix already included in the content
        var formattedContent = $"[Human]: {content}";
        var message = new StormMessage("Human", DateTime.UtcNow, formattedContent);
        session.Messages.Add(message);
    }

    private void MoveToNextAgent()
    {
        // Advance to the next agent in the rotation, wrapping around to the first agent after the last one
        currentAgentIndex = (currentAgentIndex + 1) % session.Agents.Count;
    }
    
    private static int GetNextAgentIndexFromHistory(List<StormMessage> messages, List<Agent> agentList)
    {
        // Get the last message from an agent (not from Human/user)
        var lastAgentMessage = messages
            .LastOrDefault(m => m.AgentName != "Human" && m.AgentName != "user");
            
        if (lastAgentMessage == null)
            return 0; // No agent messages, start with first agent
            
        // Find the index of the last agent who spoke
        var lastAgentIndex = agentList.FindIndex(a => a.Name == lastAgentMessage.AgentName);
        
        // If agent found, return next in rotation, otherwise return first agent
        return lastAgentIndex >= 0 
            ? (lastAgentIndex + 1) % agentList.Count 
            : 0;
    }
}
