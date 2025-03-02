namespace AIStorm.Core.Services;

using AIStorm.Core.Models;
using System.Threading.Tasks;

public interface IAIService
{
    // Core method for sending a message to an AI service
    Task<string> SendMessageAsync(Agent agent, string[] conversationHistory, string userMessage);
    
    // Get supported models from the service
    Task<string[]> GetAvailableModelsAsync();
}
