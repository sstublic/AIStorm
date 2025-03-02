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
            throw new FileNotFoundException($"File not found", path);
        return File.ReadAllText(path);
    }

    private void WriteFile(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, content);
    }

    private MarkdownSegment GetRequiredSegment(List<MarkdownSegment> segments, string type, string entityName)
    {
        var segment = serializer.FindSegment(segments, type);
        if (segment == null)
            throw new FormatException($"Invalid {entityName} format. Missing {type} tag.");
        return segment;
    }

    private string GetRequiredProperty(MarkdownSegment segment, string property)
    {
        if (!segment.Properties.TryGetValue(property, out var value))
            throw new FormatException($"Missing required property: {property}");
        return value;
    }

    private Agent CreateAgentFromSegment(MarkdownSegment segment, string name)
    {
        var aiServiceType = GetRequiredProperty(segment, "type");
        var aiModel = GetRequiredProperty(segment, "model");
        
        return new Agent(name, aiServiceType, aiModel, segment.Content);
    }

    private Agent CreateAgentFromTemplate(MarkdownSegment segment)
    {
        var name = GetRequiredProperty(segment, "name");
        var service = GetRequiredProperty(segment, "service");
        var model = GetRequiredProperty(segment, "model");
        
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
        var agentName = GetRequiredProperty(segment, "from");
        var timestampStr = GetRequiredProperty(segment, "timestamp");
        var timestamp = Tools.ParseAsUtc(timestampStr);
        
        var messageContent = segment.Content;
        var lines = messageContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > 0 && lines[0].StartsWith("## ["))
        {
            messageContent = string.Join(Environment.NewLine, lines.Skip(1)).Trim();
        }
        
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
            throw new FileNotFoundException(
                $"Session file not found. Path: {fullPath}, ID: {id}", 
                fullPath);
        }

        var fileContent = ReadFile(fullPath);
        var segments = serializer.DeserializeDocument(fileContent);
        
        var sessionSegment = GetRequiredSegment(segments, "session", "session");
        
        var created = Tools.ParseAsUtc(GetRequiredProperty(sessionSegment, "created"));
        var description = GetRequiredProperty(sessionSegment, "description");
        
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
    
    public SessionPremise LoadSessionPremise(string id)
    {
        logger.LogWarning("LoadSessionPremise is deprecated. Use LoadSession instead and access the Premise property.");
        
        try
        {
            var session = LoadSession(id);
            return session.Premise;
        }
        catch (FileNotFoundException)
        {
            var fullPath = Path.Combine(basePath, id + ".ini.md");
            
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Session premise file not found: {id}", fullPath);

            var fileContent = ReadFile(fullPath);
            var segments = serializer.DeserializeDocument(fileContent);
            
            if (segments.Count == 0)
                throw new FormatException("Invalid session premise markdown format. Missing aistorm tag.");
            
            return new SessionPremise(id, segments[0].Content);
        }
    }

    public void SaveSessionPremise(string id, SessionPremise premise)
    {
        logger.LogWarning("SaveSessionPremise is deprecated. Use SaveSession instead with a session that includes the premise.");
        
        var session = new Session(
            id: premise.Id,
            created: DateTime.UtcNow,
            description: $"Session based on premise: {premise.Id}",
            premise: premise
        );
        
        SaveSession(id, session);
    }
}
