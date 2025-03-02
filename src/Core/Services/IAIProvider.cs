namespace AIStorm.Core.Services;

using AIStorm.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IAIProvider
{
    // Core method for sending a message to an AI service
    Task<string> SendMessageAsync(Agent agent, List<StormMessage> conversationHistory, string userMessage);
    
    // Get supported models from the service
    Task<string[]> GetAvailableModelsAsync();
}
