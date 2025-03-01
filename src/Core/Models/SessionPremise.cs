namespace AIStorm.Core.Models;

public class SessionPremise
{
    public string Id { get; set; }
    public string Content { get; set; }

    public SessionPremise(string id, string content)
    {
        Id = id;
        Content = content;
    }
}
