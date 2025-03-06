namespace AIStorm.Core.AI;

using AIStorm.Core.Models;
using AIStorm.Core.SessionManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class AIMockProvider : IAIProvider
{
    private readonly ILogger<AIMockProvider> logger;
    private readonly IPromptBuilder promptBuilder;
    private readonly AIMockOptions options;
    
    public AIMockProvider(
        IOptions<AIMockOptions> options, 
        ILogger<AIMockProvider> logger,
        IPromptBuilder promptBuilder)
    {
        this.logger = logger;
        this.promptBuilder = promptBuilder;
        this.options = options.Value;
        
        logger.LogInformation("Initializing AIMockProvider");
        
        // Validate options
        if (options.Value.Models == null || !options.Value.Models.Any())
        {
            logger.LogWarning("AIMock models list is empty or null - provider may not work correctly");
        }
    }
    
    public Task<string> SendMessageAsync(Agent agent, SessionPremise premise, List<StormMessage> conversationHistory)
    {
        logger.LogInformation("Processing mock request for agent: {AgentName} using model: {Model}", 
            agent.Name, agent.AIModel);
            
        // Check which model is being used and respond accordingly
        switch (agent.AIModel)
        {
            case "AlwaysThrows":
                logger.LogWarning("Throwing mock exception as requested for AlwaysThrows model");
                throw new Exception("This is a mock exception from AIMockProvider's AlwaysThrows model");
                
            case "AlwaysReturns":
                logger.LogInformation("Returning mock response from AlwaysReturns model");
                var messageTemplate = "This is a mock response from AIMockProvider's AlwaysReturns model.\n\n" +
                               "Agent: {0}\n" + 
                               "Premise: {1}\n" +
                               "Message Count: {2}";
                
                var response = string.Format(
                    messageTemplate,
                    agent.Name,
                    premise?.Content?.Substring(0, Math.Min(50, premise?.Content?.Length ?? 0)) ?? "No premise",
                    conversationHistory?.Count ?? 0);
                
                return Task.FromResult(response);
                
            default:
                var errorMessage = $"Unknown model '{agent.AIModel}' requested in AIMockProvider";
                logger.LogError(errorMessage);
                throw new ArgumentException(errorMessage);
        }
    }
    
    public Task<string[]> GetAvailableModelsAsync()
    {
        if (options.Models == null || !options.Models.Any())
        {
            logger.LogWarning("AIMock models list is empty or null, returning empty array");
            return Task.FromResult(Array.Empty<string>());
        }
        
        logger.LogInformation("Returning {Count} mock models from configuration", options.Models.Count);
        return Task.FromResult(options.Models.ToArray());
    }

    public string GetProviderName() => AIMockOptions.ProviderName;
}
