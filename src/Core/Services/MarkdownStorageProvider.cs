namespace AIStorm.Core.Services;

using AIStorm.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

public class MarkdownStorageProvider : IStorageProvider
{
    private readonly string basePath;

    public MarkdownStorageProvider(string basePath)
    {
        this.basePath = basePath;
    }

    public Agent LoadAgent(string id)
    {
        string fullPath = Path.Combine(basePath, id);
        
        if (!File.Exists(fullPath))
        {
            return null;
        }

        string content = File.ReadAllText(fullPath);
        return ParseAgent(content, Path.GetFileNameWithoutExtension(fullPath));
    }

    public void SaveAgent(string id, Agent agent)
    {
        string fullPath = Path.Combine(basePath, id);
        string directoryPath = Path.GetDirectoryName(fullPath);
        
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string content = GenerateAgentMarkdown(agent);
        File.WriteAllText(fullPath, content);
    }

    private Agent ParseAgent(string markdown, string fileName)
    {
        var match = Regex.Match(markdown, @"<aistorm\s+type=""([^""]+)""\s+model=""([^""]+)""\s*/>");
        
        if (!match.Success)
        {
            throw new FormatException("Invalid agent markdown format. Missing or invalid aistorm tag.");
        }

        string aiServiceType = match.Groups[1].Value;
        string aiModel = match.Groups[2].Value;
        
        string systemPrompt = markdown.Substring(match.Index + match.Length).Trim();

        return new Agent(fileName, aiServiceType, aiModel, systemPrompt);
    }

    private string GenerateAgentMarkdown(Agent agent)
    {
        return $@"<aistorm type=""{agent.AIServiceType}"" model=""{agent.AIModel}"" />

{agent.SystemPrompt}";
    }

    public Session LoadSession(string id)
    {
        string fullPath = Path.Combine(basePath, id);
        
        if (!File.Exists(fullPath))
        {
            return null;
        }

        string content = File.ReadAllText(fullPath);
        return ParseSession(content, Path.GetDirectoryName(id));
    }

    public void SaveSession(string id, Session session)
    {
        string fullPath = Path.Combine(basePath, id);
        string directoryPath = Path.GetDirectoryName(fullPath);
        
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string content = GenerateSessionMarkdown(session);
        File.WriteAllText(fullPath, content);
    }

    private Session ParseSession(string markdown, string sessionId)
    {
        // Parse session metadata
        var sessionMatch = Regex.Match(markdown, @"<aistorm\s+type=""session""\s+created=""([^""]+)""\s+description=""([^""]+)""\s*/>");
        
        if (!sessionMatch.Success)
        {
            throw new FormatException("Invalid session markdown format. Missing or invalid session tag.");
        }

        string createdStr = sessionMatch.Groups[1].Value;
        string description = sessionMatch.Groups[2].Value;
        
        DateTime created = DateTime.Parse(createdStr);
        
        var session = new Session(sessionId, created, description);
        
        // Parse messages - this pattern now skips the heading line with the agent name
        var messageMatches = Regex.Matches(markdown, @"<aistorm\s+type=""message""\s+from=""([^""]+)""\s+timestamp=""([^""]+)""\s*/>(?:.*?\n){0,3}(.*?)(?=<aistorm|$)", RegexOptions.Singleline);
        
        foreach (Match match in messageMatches)
        {
            string agentName = match.Groups[1].Value;
            string timestampStr = match.Groups[2].Value;
            string content = match.Groups[3].Value.Trim();
            
            DateTime timestamp = DateTime.Parse(timestampStr);
            
            session.Messages.Add(new Message(agentName, timestamp, content));
        }
        
        return session;
    }

    private string GenerateSessionMarkdown(Session session)
    {
        var sb = new StringBuilder();
        
        // Add session metadata
        sb.AppendLine($"<aistorm type=\"session\" created=\"{session.Created:yyyy-MM-ddTHH:mm:ssZ}\" description=\"{session.Description}\" />");
        sb.AppendLine();
        sb.AppendLine($"# {session.Description}");
        sb.AppendLine();
        
        // Add messages
        foreach (var message in session.Messages)
        {
            sb.AppendLine($"<aistorm type=\"message\" from=\"{message.AgentName}\" timestamp=\"{message.Timestamp:yyyy-MM-ddTHH:mm:ssZ}\" />");
            sb.AppendLine();
            sb.AppendLine($"## [{message.AgentName}]:");
            sb.AppendLine();
            sb.AppendLine(message.Content);
            sb.AppendLine();
        }
        
        return sb.ToString();
    }
}
