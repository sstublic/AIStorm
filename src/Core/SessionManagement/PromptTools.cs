namespace AIStorm.Core.SessionManagement;

using AIStorm.Core.Models;
using System.Text.RegularExpressions;

public static class PromptTools
{
    public static string RemoveAgentNamePrefixFromMessage(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            return response;
        }

        // Regex to match the pattern "[AnyName]:" at the beginning of the string,
        // possibly followed by whitespace, and possibly repeated multiple times
        var pattern = @"^\s*(\[\s*[^\]]+\]\s*:[\s\n]*)+";
        
        return Regex.Replace(response, pattern, string.Empty);
    }
    
    public static string FormatMessageWithAgentNamePrefix(string agentName, string content)
    {
        return $"## [{agentName}]:\n\n{content}";
    }

    public static string CreateExtendedSystemPrompt(Agent agent, SessionPremise premise)
    {
        string enhancedSystemPrompt = $"You are {agent.Name}. {agent.SystemPrompt}\n\n";
        enhancedSystemPrompt += $"Context: {premise.Content}\n\n";
        
        enhancedSystemPrompt += 
            $"You will be provided with the history of the conversation so far with each participant's message prefixed by the name of the speaker in the form `[<SpeakerName>]: `\n\n" +
            "When responding, DO NOT add the prefix to your response!\n\n";
            
        return enhancedSystemPrompt;
    }
}
