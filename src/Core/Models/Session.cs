namespace AIStorm.Core.Models;

using System;
using System.Collections.Generic;

public class Session
{
    public string Id { get; set; }
    public DateTime Created { get; set; }
    public string Description { get; set; }
    public List<StormMessage> Messages { get; set; }
    public List<Agent> Agents { get; set; }
    public SessionPremise Premise { get; set; }

    public Session(string id, DateTime created, string description, SessionPremise premise)
    {
        Id = id;
        Created = created;
        Description = description;
        Premise = premise;
        Messages = new List<StormMessage>();
        Agents = new List<Agent>();
    }
}
