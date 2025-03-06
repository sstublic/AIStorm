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
    private readonly AIProviderManager providerManager;
    private readonly ILogger<SessionRunner> logger;
    private readonly Session session;
    private int currentAgentIndex = 0;

    public SessionRunner(Session session, AIProviderManager providerManager, ILogger<SessionRunner> logger)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.providerManager = providerManager ?? throw new ArgumentNullException(nameof(providerManager));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        this.currentAgentIndex = GetNextAgentIndexFromHistory(session.Messages, session.Agents);
        logger.LogInformation("Initialized SessionRunner with session: {SessionId}", session.Id);
    }

    public Session Session => session;

    public Agent NextAgentToRespond => session.Agents[currentAgentIndex];

    public async Task Next()
    {
        Agent agent = NextAgentToRespond;
        
        logger.LogDebug("Getting response from next agent: {AgentName}", agent.Name);
        
        List<StormMessage> conversationHistory = GetConversationHistory();
        
        string formattedContent;
        try
        {
            IAIProvider provider;
            try
            {
                provider = providerManager.GetProviderByName(agent.AIServiceType);
                logger.LogDebug("Using provider {ProviderName} for agent {AgentName}", 
                    agent.AIServiceType, agent.Name);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Provider {ProviderName} not found for agent {AgentName}", 
                    agent.AIServiceType, agent.Name);
                formattedContent = PromptTools.FormatErrorMessageWithAgentNamePrefix(
                    agent.Name, 
                    new Exception($"No provider available with name '{agent.AIServiceType}'"));
                session.AddMessage(new StormMessage(agent.Name, DateTime.UtcNow, formattedContent));
                MoveToNextAgent();
                return;
            }
            
            string response = await provider.SendMessageAsync(agent, session.Premise, conversationHistory);
            formattedContent = PromptTools.FormatMessageWithAgentNamePrefix(agent.Name, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting response from AI provider for agent {AgentName}", agent.Name);
            formattedContent = PromptTools.FormatErrorMessageWithAgentNamePrefix(agent.Name, ex);
        }
        
        var message = new StormMessage(agent.Name, DateTime.UtcNow, formattedContent);
        session.AddMessage(message);
        
        MoveToNextAgent();
    }

    public List<StormMessage> GetConversationHistory()
    {
        return session.Messages.ToList();
    }

    public void AddUserMessage(string content)
    {
        var formattedContent = PromptTools.FormatMessageWithAgentNamePrefix("Human", content);
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
