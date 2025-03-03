namespace AIStorm.Core.Storage.Markdown;

using System;
using AIStorm.Core.Common;

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
}
