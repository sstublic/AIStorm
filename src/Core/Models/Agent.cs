namespace AIStorm.Core.Models;

public class Agent
{
    public string Name { get; }
    public string AIServiceType { get; }
    public string AIModel { get; }
    public string SystemPrompt { get; }

    public Agent(string name, string aiServiceType, string aiModel, string systemPrompt)
    {
        Name = name;
        AIServiceType = aiServiceType;
        AIModel = aiModel;
        SystemPrompt = systemPrompt;
    }
}
