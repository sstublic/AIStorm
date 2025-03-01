namespace AIStorm.Core.Models;

public class Agent
{
    public string Name { get; set; }
    public string AIServiceType { get; set; }
    public string AIModel { get; set; }
    public string SystemPrompt { get; set; }

    public Agent(string name, string aiServiceType, string aiModel, string systemPrompt)
    {
        Name = name;
        AIServiceType = aiServiceType;
        AIModel = aiModel;
        SystemPrompt = systemPrompt;
    }
}
