namespace AIStorm.Core.AI;

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class AIProviderManager
{
    private readonly IEnumerable<IAIProvider> providers;
    private readonly ILogger<AIProviderManager> logger;

    public AIProviderManager(
        IEnumerable<IAIProvider> providers,
        ILogger<AIProviderManager> logger)
    {
        this.providers = providers ?? throw new ArgumentNullException(nameof(providers));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        logger.LogDebug("AIProviderManager initialized with {Count} providers", providers.Count());
    }

    // Gets all registered AI providers along with their available models
    // Returns a dictionary mapping provider names to their available models
    public async Task<Dictionary<string, string[]>> GetProvidersWithModelsAsync()
    {
        var result = new Dictionary<string, string[]>();
        
        foreach (var provider in providers)
        {
            try
            {
                var providerName = provider.GetProviderName();
                var models = await provider.GetAvailableModelsAsync();
                
                if (models.Length > 0)
                {
                    result[providerName] = models;
                    logger.LogDebug("Provider {ProviderName} has {ModelsCount} available models", 
                        providerName, models.Length);
                }
                else
                {
                    logger.LogWarning("Provider {ProviderName} has no available models, skipping", 
                        providerName);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving models from provider {ProviderType}", 
                    provider.GetType().Name);
            }
        }
        
        return result;
    }
}
