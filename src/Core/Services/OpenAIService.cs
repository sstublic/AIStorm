namespace AIStorm.Core.Services;

using AIStorm.Core.Models;
using AIStorm.Core.Models.AI;
using Message = AIStorm.Core.Models.AI.Message;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class OpenAIService : IAIService
{
    private readonly HttpClient httpClient;
    private readonly string apiKey;
    
    public OpenAIService(string apiKey, string baseUrl = "https://api.openai.com/v1/")
    {
        this.apiKey = apiKey;
        this.httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
        this.httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }
    
    public async Task<string> SendMessageAsync(Agent agent, string[] conversationHistory, string userMessage)
    {
        // Format conversation history according to OpenAI's API expectations
        var messages = FormatConversationForAgent(agent, conversationHistory, userMessage);
        
        // Create the request JSON directly
        var requestData = new
        {
            model = agent.AIModel,
            messages = messages,
            temperature = 0.7f
        };
        
        var content = new StringContent(
            JsonSerializer.Serialize(requestData), 
            Encoding.UTF8, 
            "application/json");
            
        var response = await httpClient.PostAsync("chat/completions", content);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadFromJsonAsync<JsonDocument>();
        // Parse response and extract the generated text
        var responseText = responseJson.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
            
        return responseText;
    }
    
    public async Task<string[]> GetAvailableModelsAsync()
    {
        var response = await httpClient.GetAsync("models");
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var models = new List<string>();
        
        var modelsArray = responseJson.RootElement.GetProperty("data");
        foreach (var model in modelsArray.EnumerateArray())
        {
            var id = model.GetProperty("id").GetString();
            if (id.StartsWith("gpt-"))
            {
                models.Add(id);
            }
        }
        
        return models.ToArray();
    }
    
    private List<AIStorm.Core.Models.AI.Message> FormatConversationForAgent(Agent agent, string[] conversationHistory, string userMessage)
    {
        var messages = new List<Message>
        {
            new Message("system", agent.SystemPrompt)
        };
        
        // Add conversation history
        foreach (var message in conversationHistory)
        {
            // Parse the message to determine the role
            if (message.StartsWith("[" + agent.Name + "]:"))
            {
                messages.Add(new Message("assistant", message));
            }
            else
            {
                messages.Add(new Message("user", message));
            }
        }
        
        // Add the latest user message
        messages.Add(new Message("user", userMessage));
        
        return messages;
    }
}
