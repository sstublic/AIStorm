namespace AIStorm.Core.Models;

using System;
using System.Collections.Generic;
using System.Linq;

public class Session
{
    public string Id { get; }
    public DateTime Created { get; }
    public IReadOnlyList<StormMessage> Messages => messages;
    public IReadOnlyList<Agent> Agents { get; }
    public SessionPremise Premise { get; }

    private readonly List<StormMessage> messages = new();

    public Session(string id, DateTime created, SessionPremise premise, IEnumerable<Agent> agents, IEnumerable<StormMessage>? messages = null)
    {
        Id = id;
        Created = created;
        Premise = premise ?? throw new ArgumentNullException(nameof(premise));
        
        if (agents == null)
            throw new ArgumentNullException(nameof(agents));
            
        var agentList = new List<Agent>(agents);
        if (agentList.Count == 0)
            throw new ArgumentException("At least one agent must be provided", nameof(agents));
            
        Agents = agentList.AsReadOnly();
        
        if (messages != null)
            this.messages.AddRange(messages);
    }

    public void AddMessage(StormMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        
        messages.Add(message);
    }
}
