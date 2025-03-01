namespace AIStorm.Core.Services;

using AIStorm.Core.Models;
using System;
using System.IO;
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
}
