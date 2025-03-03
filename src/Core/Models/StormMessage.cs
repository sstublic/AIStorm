namespace AIStorm.Core.Models;

public class StormMessage
{
    public string AgentName { get; }
    public DateTime Timestamp { get; }
    public string Content { get; }

    public StormMessage(string agentName, DateTime timestamp, string content)
    {
        AgentName = agentName;
        Timestamp = timestamp;
        Content = content;
    }
}
