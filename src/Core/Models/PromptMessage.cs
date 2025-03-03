namespace AIStorm.Core.Models;

public class PromptMessage
{
    public string Role { get; }
    public string Content { get; }
    
    public PromptMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }
}
