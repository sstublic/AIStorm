namespace AIStorm.Core.Storage.Markdown;

using System;
using System.Collections.Generic;
using System.Linq;
using AIStorm.Core.Common;
using AIStorm.Core.Models;

public class MarkdownSegment
{
    public OrderedProperties Properties { get; set; }
    public string Content { get; set; }
    
    public MarkdownSegment()
    {
        Properties = new OrderedProperties();
        Content = string.Empty;
    }
    
    public MarkdownSegment(OrderedProperties properties, string content)
    {
        Properties = properties ?? new OrderedProperties();
        Content = content ?? string.Empty;
    }
    
    public T GetProperty<T>(string key, T? defaultValue = default)
    {
        if (!Properties.TryGetValue(key, out var value))
            return defaultValue!;
            
        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue!;
        }
    }
    
    public string GetRequiredProperty(string property)
    {
        if (!Properties.TryGetValue(property, out var value))
            throw new FormatException($"Missing required property: {property}");
        return value;
    }
    
    public DateTime GetRequiredTimestampUtc(string property)
    {
        var timestampStr = GetRequiredProperty(property);
        return Tools.ParseAsUtc(timestampStr);
    }
    
    public string GetSegmentType() => GetProperty<string>("type", string.Empty);

    public Agent ToAgent()
    {
        if (GetSegmentType() != "agent")
            throw new InvalidOperationException("This segment is not an agent segment");
        
        var name = GetRequiredProperty("name");
        var service = GetRequiredProperty("service");
        var model = GetRequiredProperty("model");
        
        return new Agent(name, service, model, Content);
    }

    public SessionPremise ToPremise(string id)
    {
        if (GetSegmentType() != "premise")
            throw new InvalidOperationException("This segment is not a premise segment");
        
        return new SessionPremise(id, Content);
    }

    public StormMessage ToStormMessage()
    {
        if (GetSegmentType() != "message")
            throw new InvalidOperationException("This segment is not a message segment");
        
        var agentName = GetRequiredProperty("from");
        var timestamp = GetRequiredTimestampUtc("timestamp");
        return new StormMessage(agentName, timestamp, Content);
    }

    public static MarkdownSegment FromAgent(Agent agent)
    {
        return new MarkdownSegment(
            new OrderedProperties(
                ("type", "agent"),
                ("name", agent.Name),
                ("service", agent.AIServiceType),
                ("model", agent.AIModel)
            ),
            agent.SystemPrompt
        );
    }

    public static MarkdownSegment FromPremise(SessionPremise premise)
    {
        return new MarkdownSegment(
            new OrderedProperties(
                ("type", "premise")
            ),
            premise.Content
        );
    }

    public static MarkdownSegment FromSessionMetadata(string id, Session session)
    {
        return new MarkdownSegment(
            new OrderedProperties(
                ("type", "session"),
                ("created", Tools.UtcToString(session.Created))
            ),
            $"# Session {id}"
        );
    }

    public static MarkdownSegment FromMessage(StormMessage message)
    {
        return new MarkdownSegment(
            new OrderedProperties(
                ("type", "message"),
                ("from", message.AgentName),
                ("timestamp", Tools.UtcToString(message.Timestamp))
            ),
            message.Content
        );
    }
    
    public string ToMarkdown(MarkdownSerializer serializer)
    {
        return serializer.SerializeDocument(new List<MarkdownSegment> { this });
    }
    
    public static List<MarkdownSegment> ParseSegments(string markdownContent, MarkdownSerializer serializer, bool throwOnNone = false)
    {
        if (string.IsNullOrWhiteSpace(markdownContent))
        {
            if (throwOnNone)
                throw new FormatException("Invalid markdown format. No content provided.");
                
            return new List<MarkdownSegment>();
        }
        
        var segments = serializer.DeserializeDocument(markdownContent);
        
        if (throwOnNone && segments.Count == 0)
            throw new FormatException("Invalid markdown format. No segments found.");
            
        return segments;
    }
}
