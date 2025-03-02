namespace AIStorm.Core.Services;

using AIStorm.Core.Models;
using System.Text.RegularExpressions;

public static class PromptTools
{
    public static string CleanupResponse(string response)
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

    public static string CreateExtendedSystemPrompt(Agent agent)
    {
        string enhancedSystemPrompt = $"You are {agent.Name}. {agent.SystemPrompt}\n\n" +
            $"You will be provided with the history of the conversation so far with each participant's message prefixed by the name of the speaker in the form `[<SpeakerName>]: `\n\n" +
            "When responding, DO NOT add the prefix to your response!\n\n";
            
        return enhancedSystemPrompt;
    }
}
