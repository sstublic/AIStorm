namespace AIStorm.Core.Storage.Markdown;

using AIStorm.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AIStorm.Core.Common;
using AIStorm.Core.Storage;

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
        try
        {
            var name = segment.GetRequiredProperty("name");
            var service = segment.GetRequiredProperty("service");
            var model = segment.GetRequiredProperty("model");
            
            return new Agent(name, service, model, segment.Content);
        }
        catch (Exception ex) when (ex is FormatException || ex is ArgumentException)
        {
            throw new FormatException($"Invalid agent template format: {ex.Message}", ex);
        }
    }

    private List<MarkdownSegment> CreateAgentSegments(IEnumerable<Agent> agents)
    {
        return agents.Select(agent => 
            new MarkdownSegment(
                new OrderedProperties(
                    ("type", "agent"),
                    ("name", agent.Name),
                    ("service", agent.AIServiceType),
                    ("model", agent.AIModel)
                ),
                agent.SystemPrompt
            )
        ).ToList();
    }

    private List<MarkdownSegment> CreateMessageSegments(IEnumerable<StormMessage> messages)
    {
        return messages.Select(message => 
            new MarkdownSegment(
                new OrderedProperties(
                    ("type", "message"),
                    ("from", message.AgentName),
                    ("timestamp", Tools.UtcToString(message.Timestamp))
                ),
                $"## [{message.AgentName}]:\n\n{message.Content}"
            )
        ).ToList();
    }

    private StormMessage CreateMessageFromSegment(MarkdownSegment segment)
    {
        try
        {
            var agentName = segment.GetRequiredProperty("from");
            var timestamp = segment.GetRequiredTimestampUtc("timestamp");
            return new StormMessage(agentName, timestamp, segment.Content);
        }
        catch (Exception ex) when (ex is FormatException || ex is ArgumentException)
        {
            throw new FormatException($"Invalid message format: {ex.Message}", ex);
        }
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
        
        var agents = serializer.FindSegments(segments, "agent")
            .Select(segment => CreateAgentFromTemplate(segment))
            .ToList();
        
        var messages = serializer.FindSegments(segments, "message")
            .Select(segment => CreateMessageFromSegment(segment))
            .ToList();
        
        return new Session(id, created, premise, agents, messages);
    }

    public void SaveSession(string id, Session session)
    {
        var fullPath = GetSessionPath(id);
        
        var segments = new List<MarkdownSegment>();
        
        var sessionProperties = new OrderedProperties(
            ("type", "session"),
            ("created", Tools.UtcToString(session.Created))
        );
        
        var sessionContent = $"# Session {id}";
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
