namespace AIStorm.Core.Storage;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public class MarkdownSerializer
{
    public List<MarkdownSegment> DeserializeDocument(string markdown)
    {
        var segments = new List<MarkdownSegment>();
        
        // Find all <aistorm> tags and their content
        var matches = Regex.Matches(markdown, @"<aistorm\s+(.*?)\s*/>(?:[\s\S]*?)(?=<aistorm|$)", RegexOptions.Singleline);
        
        foreach (Match match in matches)
        {
            var tagContent = match.Groups[1].Value;
            var properties = ParseTagProperties(tagContent);
            
            // Extract content after the tag until the next tag or end of document
            var fullMatch = match.Value;
            var tagEnd = fullMatch.IndexOf("/>") + 2;
            var content = fullMatch.Substring(tagEnd).Trim();
            
            segments.Add(new MarkdownSegment(properties, content));
        }
        
        return segments;
    }
    
    public string SerializeDocument(List<MarkdownSegment> segments)
    {
        var sb = new StringBuilder();
        
        foreach (var segment in segments)
        {
            // Generate tag with properties
            sb.AppendLine(GenerateTagWithProperties(segment.Properties));
            sb.AppendLine();
            
            // Add content
            if (!string.IsNullOrEmpty(segment.Content))
            {
                sb.AppendLine(segment.Content);
                sb.AppendLine();
            }
        }
        
        return sb.ToString().TrimEnd();
    }
    
    private OrderedProperties ParseTagProperties(string tagContent)
    {
        var properties = new OrderedProperties();
        
        // Match all property="value" pairs
        var matches = Regex.Matches(tagContent, @"(\w+)=""([^""]*)""\s*");
        
        foreach (Match match in matches)
        {
            var key = match.Groups[1].Value;
            var value = match.Groups[2].Value;
            properties.Add(key, value);
        }
        
        return properties;
    }
    
    private string GenerateTagWithProperties(OrderedProperties properties)
    {
        var sb = new StringBuilder("<aistorm ");
        
        foreach (var prop in properties.GetProperties())
        {
            sb.Append($"{prop.Key}=\"{prop.Value}\" ");
        }
        
        sb.Append("/>");
        return sb.ToString();
    }
    
    public MarkdownSegment? FindSegment(List<MarkdownSegment> segments, string typeValue)
    {
        return segments.FirstOrDefault(s => 
            s.Properties.TryGetValue("type", out var type) && type == typeValue);
    }
    
    public List<MarkdownSegment> FindSegments(List<MarkdownSegment> segments, string typeValue)
    {
        return segments.Where(s => 
            s.Properties.TryGetValue("type", out var type) && type == typeValue).ToList();
    }
}
