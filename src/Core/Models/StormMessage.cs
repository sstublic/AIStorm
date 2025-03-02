namespace AIStorm.Core.Models;

public class StormMessage
{
    public string AgentName { get; set; }
    public DateTime Timestamp { get; set; }
    public string Content { get; set; }

    public StormMessage(string agentName, DateTime timestamp, string content)
    {
        AgentName = agentName;
        Timestamp = timestamp;
        Content = content;
    }
}
