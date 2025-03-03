namespace AIStorm.Core.Storage;

using System;
using System.Collections.Generic;
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
    
    // Add a constructor that accepts Dictionary for backward compatibility
    public MarkdownSegment(Dictionary<string, string> properties, string content)
        : this(ConvertToOrderedProperties(properties), content)
    {
    }
    
    private static OrderedProperties ConvertToOrderedProperties(Dictionary<string, string> dict)
    {
        if (dict == null)
            return new OrderedProperties();
            
        var ordered = new OrderedProperties();
        foreach (var kvp in dict)
        {
            ordered.Add(kvp.Key, kvp.Value);
        }
        return ordered;
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
