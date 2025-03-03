namespace AIStorm.Core.Models;

public class SessionPremise
{
    public string Id { get; }
    public string Content { get; }

    public SessionPremise(string id, string content)
    {
        Id = id;
        Content = content;
    }
}
