namespace AIStorm.Core.Services;

using AIStorm.Core.Models;
using AIStorm.Core.Services.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class MarkdownStorageProvider : IStorageProvider
{
    private readonly string basePath;
    private readonly MarkdownSerializer serializer;

    public MarkdownStorageProvider(IOptions<MarkdownStorageOptions> options, MarkdownSerializer serializer)
    {
        var storageOptions = options.Value;
        
        // Validate options
        if (string.IsNullOrEmpty(storageOptions.BasePath))
            throw new ArgumentException("Base path is required", nameof(options));
            
        this.basePath = storageOptions.BasePath;
        this.serializer = serializer;
    }

    public Agent LoadAgent(string id)
    {
        string fullPath = Path.Combine(basePath, id);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Agent file not found: {id}", fullPath);
        }

        string content = File.ReadAllText(fullPath);
        var segments = serializer.DeserializeDocument(content);
        
        if (segments.Count == 0)
        {
            throw new FormatException("Invalid agent markdown format. Missing aistorm tag.");
        }
        
        var agentSegment = segments[0];
        
        // For agent files, the type property is the AI service type
        var aiServiceType = agentSegment.Properties.ContainsKey("type") ? 
            agentSegment.Properties["type"] : 
            throw new FormatException("Invalid agent markdown format. Missing type property.");
            
        var aiModel = agentSegment.Properties.ContainsKey("model") ? 
            agentSegment.Properties["model"] : 
            throw new FormatException("Invalid agent markdown format. Missing model property.");
        
        return new Agent(
            Path.GetFileNameWithoutExtension(fullPath),
            aiServiceType,
            aiModel,
            agentSegment.Content
        );
    }

    public void SaveAgent(string id, Agent agent)
    {
        var fullPath = Path.Combine(basePath, id);
        var directoryPath = Path.GetDirectoryName(fullPath);
        
        if (directoryPath != null && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var properties = new OrderedProperties();
        properties.Add("type", agent.AIServiceType);
        properties.Add("model", agent.AIModel);
        
        var segment = new MarkdownSegment(properties, agent.SystemPrompt);
        var content = serializer.SerializeDocument(new List<MarkdownSegment> { segment });
        
        File.WriteAllText(fullPath, content);
    }

    public Session LoadSession(string id)
    {
        // Ensure the id has the .log.md extension
        string sessionPath = id;
        if (!sessionPath.EndsWith(".log.md"))
        {
            // If id already has .md extension, replace it with .log.md
            if (sessionPath.EndsWith(".md"))
            {
                sessionPath = sessionPath.Substring(0, sessionPath.Length - 3) + ".log.md";
            }
            else
            {
                // Otherwise, append .log.md
                sessionPath = sessionPath + ".log.md";
            }
        }
        
        string fullPath = Path.Combine(basePath, sessionPath);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Session file not found: {sessionPath}", fullPath);
        }

        string fileContent = File.ReadAllText(fullPath);
        var segments = serializer.DeserializeDocument(fileContent);
        
        // Find session metadata segment
        var sessionSegment = serializer.FindSegment(segments, "session");
        if (sessionSegment == null)
        {
            throw new FormatException("Invalid session markdown format. Missing session tag.");
        }
        
        if (!sessionSegment.Properties.TryGetValue("created", out var createdStr) ||
            !sessionSegment.Properties.TryGetValue("description", out var description))
        {
            throw new FormatException("Invalid session markdown format. Missing required properties.");
        }
        
        var created = Tools.ParseAsUtc(createdStr);
        
        // Use the filename without extension as the session ID
        var sessionId = Path.GetFileNameWithoutExtension(fullPath);
        // Remove .log suffix if present
        if (sessionId.EndsWith(".log"))
        {
            sessionId = sessionId.Substring(0, sessionId.Length - 4);
        }
        
        var session = new Session(sessionId, created, description);
        
        // Find message segments
        var messageSegments = serializer.FindSegments(segments, "message");
        foreach (var messageSegment in messageSegments)
        {
            if (!messageSegment.Properties.TryGetValue("from", out var agentName) ||
                !messageSegment.Properties.TryGetValue("timestamp", out var timestampStr))
            {
                continue; // Skip invalid messages
            }
            
            var timestamp = Tools.ParseAsUtc(timestampStr);
            var messageContent = messageSegment.Content;
            
            // Extract actual message content by removing the heading line if present
            var lines = messageContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 0 && lines[0].StartsWith("## ["))
            {
                messageContent = string.Join(Environment.NewLine, lines.Skip(1)).Trim();
            }
            
            session.Messages.Add(new Message(agentName, timestamp, messageContent));
        }
        
        return session;
    }

    public void SaveSession(string id, Session session)
    {
        // Ensure the id has the .log.md extension
        string sessionPath = id;
        if (!sessionPath.EndsWith(".log.md"))
        {
            // If id already has .md extension, replace it with .log.md
            if (sessionPath.EndsWith(".md"))
            {
                sessionPath = sessionPath.Substring(0, sessionPath.Length - 3) + ".log.md";
            }
            else
            {
                // Otherwise, append .log.md
                sessionPath = sessionPath + ".log.md";
            }
        }
        
        var fullPath = Path.Combine(basePath, sessionPath);
        var directoryPath = Path.GetDirectoryName(fullPath);
        
        if (directoryPath != null && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var segments = new List<MarkdownSegment>();
        
        // Add session metadata segment
        var sessionProperties = new OrderedProperties();
        sessionProperties.Add("type", "session");
        sessionProperties.Add("created", Tools.UtcToString(session.Created));
        sessionProperties.Add("description", session.Description);
        
        var sessionContent = $"# {session.Description}";
        segments.Add(new MarkdownSegment(sessionProperties, sessionContent));
        
        // Add message segments
        foreach (var message in session.Messages)
        {
            var messageProperties = new OrderedProperties();
            messageProperties.Add("type", "message");
            messageProperties.Add("from", message.AgentName);
            messageProperties.Add("timestamp", Tools.UtcToString(message.Timestamp));
            
            var messageContent = $"## [{message.AgentName}]:\n\n{message.Content}";
            segments.Add(new MarkdownSegment(messageProperties, messageContent));
        }
        
        var content = serializer.SerializeDocument(segments);
        File.WriteAllText(fullPath, content);
    }
    
    public SessionPremise LoadSessionPremise(string id)
    {
        // Ensure the id has the .ini.md extension
        string premisePath = id;
        if (!premisePath.EndsWith(".ini.md"))
        {
            // If id already has .md extension, replace it with .ini.md
            if (premisePath.EndsWith(".md"))
            {
                premisePath = premisePath.Substring(0, premisePath.Length - 3) + ".ini.md";
            }
            else
            {
                // Otherwise, append .ini.md
                premisePath = premisePath + ".ini.md";
            }
        }
        
        string fullPath = Path.Combine(basePath, premisePath);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Session premise file not found: {premisePath}", fullPath);
        }

        string fileContent = File.ReadAllText(fullPath);
        var segments = serializer.DeserializeDocument(fileContent);
        
        // Find the first segment (should be the only one)
        if (segments.Count == 0)
        {
            throw new FormatException("Invalid session premise markdown format. Missing aistorm tag.");
        }
        
        var premiseSegment = segments[0];
        
        // Use the filename without extension as the session ID
        var sessionId = Path.GetFileNameWithoutExtension(fullPath);
        // Remove .ini suffix if present
        if (sessionId.EndsWith(".ini"))
        {
            sessionId = sessionId.Substring(0, sessionId.Length - 4);
        }
        
        return new SessionPremise(sessionId, premiseSegment.Content);
    }

    public void SaveSessionPremise(string id, SessionPremise premise)
    {
        // Ensure the id has the .ini.md extension
        string premisePath = id;
        if (!premisePath.EndsWith(".ini.md"))
        {
            // If id already has .md extension, replace it with .ini.md
            if (premisePath.EndsWith(".md"))
            {
                premisePath = premisePath.Substring(0, premisePath.Length - 3) + ".ini.md";
            }
            else
            {
                // Otherwise, append .ini.md
                premisePath = premisePath + ".ini.md";
            }
        }
        
        var fullPath = Path.Combine(basePath, premisePath);
        var directoryPath = Path.GetDirectoryName(fullPath);
        
        if (directoryPath != null && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var segments = new List<MarkdownSegment>();
        
        // Add empty aistorm tag segment with the content
        var properties = new OrderedProperties();
        segments.Add(new MarkdownSegment(properties, premise.Content));
        
        var content = serializer.SerializeDocument(segments);
        File.WriteAllText(fullPath, content);
    }
}
