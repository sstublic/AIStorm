namespace AIStorm.Core.Services;

using AIStorm.Core.Models;
using AIStorm.Core.Models.AI;
using AIStorm.Core.Services.Options;
using Message = AIStorm.Core.Models.AI.Message;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class OpenAIProvider : IAIProvider
{
    private readonly HttpClient httpClient;
    
    public OpenAIProvider(IOptions<OpenAIOptions> options)
    {
        var openAIOptions = options.Value;
        
        // Validate options
        if (string.IsNullOrEmpty(openAIOptions.ApiKey))
            throw new ArgumentException("API key is required", nameof(options));
            
        this.httpClient = new HttpClient
        {
            BaseAddress = new Uri(openAIOptions.BaseUrl)
        };
        this.httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAIOptions.ApiKey}");
    }
    
    public async Task<string> SendMessageAsync(Agent agent, List<Message> conversationHistory, string userMessage)
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
    
    private List<OpenAIMessage> FormatConversationForAgent(Agent agent, List<AIStorm.Core.Models.AI.Message> conversationHistory, string userMessage)
    {
        var messages = new List<OpenAIMessage>
        {
            new OpenAIMessage("system", agent.SystemPrompt)
        };
        
        // Add conversation history
        foreach (var message in conversationHistory)
        {
            // Determine role based on the agent name
            string role = message.AgentName == agent.Name ? "assistant" : "user";
            messages.Add(new OpenAIMessage(role, $"[{message.AgentName}]: {message.Content}"));
        }
        
        // Add the latest user message
        messages.Add(new OpenAIMessage("user", userMessage));
        
        return messages;
    }
    
    // Private class for OpenAI messages
    private class OpenAIMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
        
        public OpenAIMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }
}
