namespace AIStorm.Core.Models.AI;

public class Message
{
    public string AgentName { get; set; }
    public string Content { get; set; }
    
    public Message(string agentName, string content)
    {
        AgentName = agentName;
        Content = content;
    }
}
