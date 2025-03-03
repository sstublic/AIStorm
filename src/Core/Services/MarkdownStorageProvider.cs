namespace AIStorm.Core.Services;

using AIStorm.Core.Models;
using AIStorm.Core.Services.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class MarkdownStorageProvider : IStorageProvider
{
    private readonly string basePath;
    private readonly string agentTemplatesPath;
    private readonly string sessionsPath;
    private readonly MarkdownSerializer serializer;
    private readonly ILogger<MarkdownStorageProvider> logger;

    public MarkdownStorageProvider(
        IOptions<MarkdownStorageOptions> options, 
        MarkdownSerializer serializer,
        ILogger<MarkdownStorageProvider> logger)
    {
        var storageOptions = options.Value;
        
        if (string.IsNullOrEmpty(storageOptions.BasePath))
            throw new ArgumentException("Base path is required", nameof(options));
            
        this.basePath = storageOptions.BasePath;
        this.agentTemplatesPath = Path.Combine(this.basePath, "AgentTemplates");
        this.sessionsPath = Path.Combine(this.basePath, "Sessions");
        this.serializer = serializer;
        this.logger = logger;
        
        Directory.CreateDirectory(this.agentTemplatesPath);
        Directory.CreateDirectory(this.sessionsPath);
        
        logger.LogInformation("MarkdownStorageProvider initialized with base path: {BasePath}", 
            Path.GetFullPath(this.basePath));
    }
    
    private string GetAgentPath(string id) 
    {
        return Path.Combine(agentTemplatesPath, id + ".md");
    }

    private string GetSessionPath(string id) 
    {
        return Path.Combine(sessionsPath, id + ".session.md");
    }

    private string ReadFile(string path)
    {
        if (!File.Exists(path))
        {
            var absolutePath = Path.GetFullPath(path);
            throw new FileNotFoundException(
                $"File not found. Relative path: {path}, Absolute path: {absolutePath}", 
                path);
        }
        return File.ReadAllText(path);
    }

    private void WriteFile(string path, string content)
    {
        string? directoryName = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }
        File.WriteAllText(path, content);
    }

    private MarkdownSegment GetRequiredSegment(List<MarkdownSegment> segments, string type, string entityName)
    {
        var segment = serializer.FindSegment(segments, type);
        if (segment == null)
            throw new FormatException($"Invalid {entityName} format. Missing {type} tag.");
        return segment;
    }

    private Agent CreateAgentFromSegment(MarkdownSegment segment, string name)
    {
        var aiServiceType = segment.GetRequiredProperty("type");
        var aiModel = segment.GetRequiredProperty("model");
        
        return new Agent(name, aiServiceType, aiModel, segment.Content);
    }

    private Agent CreateAgentFromTemplate(MarkdownSegment segment)
    {
        var name = segment.GetRequiredProperty("name");
        var service = segment.GetRequiredProperty("service");
        var model = segment.GetRequiredProperty("model");
        
        return new Agent(name, service, model, segment.Content);
    }

    private List<MarkdownSegment> CreateAgentSegments(IEnumerable<Agent> agents)
    {
        var segments = new List<MarkdownSegment>();
        
        foreach (var agent in agents)
        {
            var properties = new OrderedProperties(
                ("type", "agent"),
                ("name", agent.Name),
                ("service", agent.AIServiceType),
                ("model", agent.AIModel)
            );
            
            segments.Add(new MarkdownSegment(properties, agent.SystemPrompt));
        }
        
        return segments;
    }

    private List<MarkdownSegment> CreateMessageSegments(IEnumerable<StormMessage> messages)
    {
        var segments = new List<MarkdownSegment>();
        
        foreach (var message in messages)
        {
            var properties = new OrderedProperties(
                ("type", "message"),
                ("from", message.AgentName),
                ("timestamp", Tools.UtcToString(message.Timestamp))
            );
            
            var messageContent = $"## [{message.AgentName}]:\n\n{message.Content}";
            segments.Add(new MarkdownSegment(properties, messageContent));
        }
        
        return segments;
    }

    private StormMessage CreateMessageFromSegment(MarkdownSegment segment)
    {
        var agentName = segment.GetRequiredProperty("from");
        var timestamp = segment.GetRequiredTimestampUtc("timestamp");
        
        // Keep the original message content including the agent prefix
        // This is important for AI to understand which message came from which participant
        var messageContent = segment.Content;
        
        return new StormMessage(agentName, timestamp, messageContent);
    }

    public Agent LoadAgent(string id)
    {
        var fullPath = GetAgentPath(id);
        var content = ReadFile(fullPath);
        
        var segments = serializer.DeserializeDocument(content);
        if (segments.Count == 0)
            throw new FormatException("Invalid agent markdown format. Missing aistorm tag.");
        
        return CreateAgentFromSegment(segments[0], id);
    }

    public void SaveAgent(string id, Agent agent)
    {
        var fullPath = GetAgentPath(id);
        
        var properties = new OrderedProperties(
            ("type", agent.AIServiceType),
            ("model", agent.AIModel)
        );
        
        var segment = new MarkdownSegment(properties, agent.SystemPrompt);
        var content = serializer.SerializeDocument(new List<MarkdownSegment> { segment });
        
        WriteFile(fullPath, content);
    }

    public Session LoadSession(string id)
    {
        logger.LogInformation("LoadSession called with ID: '{SessionId}'", id);
        
        var fullPath = GetSessionPath(id);
        logger.LogInformation("Attempting to load session from path: '{FullPath}'", fullPath);
        
        if (!File.Exists(fullPath))
        {
            var absolutePath = Path.GetFullPath(fullPath);
            throw new FileNotFoundException(
                $"Session file not found. Relative path: {fullPath}, Absolute path: {absolutePath}, ID: {id}", 
                fullPath);
        }

        var fileContent = ReadFile(fullPath);
        var segments = serializer.DeserializeDocument(fileContent);
        
        var sessionSegment = GetRequiredSegment(segments, "session", "session");
        
        var created = sessionSegment.GetRequiredTimestampUtc("created");
        var description = sessionSegment.GetRequiredProperty("description");
        
        var premiseSegment = GetRequiredSegment(segments, "premise", "session");
        var premise = new SessionPremise(id, premiseSegment.Content);
        
        var session = new Session(id, created, description, premise);
        
        var agentSegments = serializer.FindSegments(segments, "agent");
        foreach (var agentSegment in agentSegments)
        {
            try {
                var agent = CreateAgentFromTemplate(agentSegment);
                session.Agents.Add(agent);
            }
            catch (FormatException ex) {
                logger.LogWarning("Invalid agent segment found in session {SessionId}: {Error}", id, ex.Message);
                continue;
            }
        }
        
        var messageSegments = serializer.FindSegments(segments, "message");
        foreach (var messageSegment in messageSegments)
        {
            try {
                var message = CreateMessageFromSegment(messageSegment);
                session.Messages.Add(message);
            }
            catch (FormatException ex) {
                logger.LogWarning("Invalid message segment found in session {SessionId}: {Error}", id, ex.Message);
                continue;
            }
        }
        
        return session;
    }

    public void SaveSession(string id, Session session)
    {
        var fullPath = GetSessionPath(id);
        
        var segments = new List<MarkdownSegment>();
        
        var sessionProperties = new OrderedProperties(
            ("type", "session"),
            ("created", Tools.UtcToString(session.Created)),
            ("description", session.Description)
        );
        
        var sessionContent = $"# {session.Description}";
        segments.Add(new MarkdownSegment(sessionProperties, sessionContent));
        
        if (session.Premise != null)
        {
            var premiseProperties = new OrderedProperties(
                ("type", "premise")
            );
            segments.Add(new MarkdownSegment(premiseProperties, session.Premise.Content));
        }
        
        segments.AddRange(CreateAgentSegments(session.Agents));
        segments.AddRange(CreateMessageSegments(session.Messages));
        
        var content = serializer.SerializeDocument(segments);
        WriteFile(fullPath, content);
    }
    
}
