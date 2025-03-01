namespace AIStorm.Core.Services;

using System;
using System.Collections.Generic;

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
    
    public T GetProperty<T>(string key, T defaultValue = default)
    {
        if (!Properties.TryGetValue(key, out var value))
            return defaultValue;
            
        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }
}
