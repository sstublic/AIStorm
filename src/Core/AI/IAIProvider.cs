namespace AIStorm.Core.AI;

using AIStorm.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IAIProvider
{
    // Core method for sending a message to an AI service
    Task<string> SendMessageAsync(Agent agent, SessionPremise premise, List<StormMessage> conversationHistory);
    
    // Get supported models from the service
    Task<string[]> GetAvailableModelsAsync();
}
