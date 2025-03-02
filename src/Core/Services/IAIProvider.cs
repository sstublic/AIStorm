namespace AIStorm.Core.Services;

using AIStorm.Core.Models;
using AIStorm.Core.Models.AI;
using Message = AIStorm.Core.Models.AI.Message;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IAIProvider
{
    // Core method for sending a message to an AI service
    Task<string> SendMessageAsync(Agent agent, List<AIStorm.Core.Models.AI.Message> conversationHistory, string userMessage);
    
    // Get supported models from the service
    Task<string[]> GetAvailableModelsAsync();
}
