namespace AIStorm.Core.Models;

using System;
using System.Collections.Generic;

public class Session
{
    public string Id { get; set; }
    public DateTime Created { get; set; }
    public string Description { get; set; }
    public List<Message> Messages { get; set; }

    public Session(string id, DateTime created, string description)
    {
        Id = id;
        Created = created;
        Description = description;
        Messages = new List<Message>();
    }
}
