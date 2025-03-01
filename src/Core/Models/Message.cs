namespace AIStorm.Core.Models;

public class Message
{
    public string AgentName { get; set; }
    public DateTime Timestamp { get; set; }
    public string Content { get; set; }

    public Message(string agentName, DateTime timestamp, string content)
    {
        AgentName = agentName;
        Timestamp = timestamp;
        Content = content;
    }
}
