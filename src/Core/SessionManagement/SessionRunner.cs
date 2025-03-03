namespace AIStorm.Core.SessionManagement;

using AIStorm.Core.Models;
using AIStorm.Core.AI;
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

    public SessionRunner(IEnumerable<Agent> agents, SessionPremise premise, IAIProvider aiProvider, ILogger<SessionRunner> logger, Session? existingSession = null)
    {
        agents = agents ?? throw new ArgumentNullException(nameof(agents));
        
        if (!agents.Any())
            throw new ArgumentException("At least one agent must be provided", nameof(agents));
        
        premise = premise ?? throw new ArgumentNullException(nameof(premise));
        this.aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        if (existingSession != null)
        {
            this.session = existingSession;
            this.currentAgentIndex = GetNextAgentIndexFromHistory(session.Messages, session.Agents);
            logger.LogInformation("Initialized SessionRunner with existing session: {SessionId}", session.Id);
        }
        else
        {
            this.session = new Session(
                id: premise.Id,
                created: DateTime.UtcNow,
                premise: premise,
                agents: agents
            );
            
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
        
        string response = await aiProvider.SendMessageAsync(agent, session.Premise, conversationHistory);
        
        var message = new StormMessage(agent.Name, DateTime.UtcNow, response);
        
        session.AddMessage(message);
        
        MoveToNextAgent();
    }

    public List<StormMessage> GetConversationHistory()
    {
        return session.Messages.ToList();
    }

    public void AddUserMessage(string content)
    {
        var formattedContent = $"[Human]: {content}";
        var message = new StormMessage("Human", DateTime.UtcNow, formattedContent);
        session.AddMessage(message);
    }

    private void MoveToNextAgent()
    {
        currentAgentIndex = (currentAgentIndex + 1) % session.Agents.Count;
    }
    
    private static int GetNextAgentIndexFromHistory(IReadOnlyList<StormMessage> messages, IReadOnlyList<Agent> agentList)
    {
        var lastAgentMessage = messages
            .LastOrDefault(m => m.AgentName != "Human" && m.AgentName != "user");
            
        if (lastAgentMessage == null)
            return 0;
            
        var lastAgentIndex = agentList
            .Select((agent, index) => new { agent.Name, Index = index })
            .FirstOrDefault(x => x.Name == lastAgentMessage.AgentName)?.Index ?? -1;
        
        return lastAgentIndex >= 0 
            ? (lastAgentIndex + 1) % agentList.Count 
            : 0;
    }
}
