namespace AIStorm.Core.SessionManagement;

using AIStorm.Core.Models;
using System;
using System.Text.RegularExpressions;

public static class PromptTools
{
    public static string RemoveAgentNamePrefixFromMessage(string response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (string.IsNullOrEmpty(response))
        {
            return string.Empty;
        }

        // Enhanced regex to match both:
        // 1. Standard prefix: "[AnyName]:" possibly with whitespace
        // 2. Markdown header: "## [AnyName]:" followed by newlines
        var pattern = @"^(?:\#\#\s*)?(\[\s*[^\]]+\]\s*:[\s\n]*)+";
        
        return Regex.Replace(response, pattern, string.Empty);
    }
    
    public static string FormatMessageWithAgentNamePrefix(string agentName, string content)
    {
        return $"## [{agentName}]:\n\n{content}";
    }
    
    public static string FormatErrorMessageWithAgentNamePrefix(string agentName, Exception exception)
    {
        var basePrefix = $"## [{agentName}]: ";
        var errorContent = ">>>ERROR FETCHING RESPONSE<<<";
        
        if (exception != null)
        {
            errorContent += $"\n\n{exception.GetType().Name}: {exception.Message}";
            
            // Add nested exception messages if available
            var innerException = exception.InnerException;
            while (innerException != null)
            {
                errorContent += $"\n{innerException.GetType().Name}: {innerException.Message}";
                innerException = innerException.InnerException;
            }
        }
        
        return basePrefix + errorContent;
    }

    public static string CreateExtendedSystemPrompt(Agent agent, SessionPremise premise)
    {
        string enhancedSystemPrompt = $"# You are {agent.Name}. {agent.SystemPrompt}\n\n";
        enhancedSystemPrompt += $"## Context of the conversation\n{premise.Content}\n\n";
        
        enhancedSystemPrompt += 
            $"You will be provided with the history of the conversation so far with each participant's message prefixed by the name of the speaker in the form `[<SpeakerName>]: `\n\n" +
            "When responding, DO NOT add the prefix to your response!\n\n" +
            "Continue conversation when asked for a response even without further user input.\n\n";;
            
        return enhancedSystemPrompt;
    }
}
