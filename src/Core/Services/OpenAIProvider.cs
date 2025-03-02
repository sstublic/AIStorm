namespace AIStorm.Core.Services;

using AIStorm.Core.Models;
using AIStorm.Core.Services.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class OpenAIProvider : IAIProvider
{
    private readonly HttpClient httpClient;
    
    public OpenAIProvider(IOptions<OpenAIOptions> options)
    {
        var openAIOptions = options.Value;
        
        // Validate options
        if (string.IsNullOrEmpty(openAIOptions.ApiKey))
        {
            throw new ArgumentException("OpenAI API key is missing. Please provide a valid API key in your configuration. ", nameof(options));
        }
            
        this.httpClient = new HttpClient
        {
            BaseAddress = new Uri(openAIOptions.BaseUrl)
        };
        this.httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAIOptions.ApiKey}");
    }
    
    public async Task<string> SendMessageAsync(Agent agent, List<StormMessage> conversationHistory, string userMessage)
    {
        try
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
            
            // If the request fails, try to get more details about the error
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"OpenAI API returned {(int)response.StatusCode} ({response.StatusCode}). " +
                    $"Details: {errorContent}. " +
                    "Please check your API key and configuration."
                );
            }
        
            var responseJson = await response.Content.ReadFromJsonAsync<JsonDocument>();
            // Parse response and extract the generated text
            var responseText = responseJson?.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
                
            return responseText ?? string.Empty;
        }
        catch (HttpRequestException)
        {
            // Pass through our custom error message
            throw;
        }
        catch (Exception ex)
        {
            // Wrap other exceptions for more context
            throw new Exception($"Error communicating with OpenAI API: {ex.Message}", ex);
        }
    }
    
    public async Task<string[]> GetAvailableModelsAsync()
    {
        try
        {
            var response = await httpClient.GetAsync("models");
            
            // If the request fails, try to get more details about the error
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"OpenAI API returned {(int)response.StatusCode} ({response.StatusCode}). " +
                    $"Details: {errorContent}. " +
                    "Please check your API key and configuration."
                );
            }
            
            var responseJson = await response.Content.ReadFromJsonAsync<JsonDocument>();
            var models = new List<string>();
            
            if (responseJson != null)
            {
                var modelsArray = responseJson.RootElement.GetProperty("data");
                foreach (var model in modelsArray.EnumerateArray())
                {
                    var id = model.GetProperty("id").GetString();
                    if (id != null && id.StartsWith("gpt-"))
                    {
                        models.Add(id);
                    }
                }
            }
            
            return models.ToArray();
        }
        catch (HttpRequestException)
        {
            // Pass through our custom error message
            throw;
        }
        catch (Exception ex)
        {
            // Wrap other exceptions for more context
            throw new Exception($"Error retrieving models from OpenAI API: {ex.Message}", ex);
        }
    }
    
    private List<OpenAIMessage> FormatConversationForAgent(Agent agent, List<StormMessage> conversationHistory, string userMessage)
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
        [JsonPropertyName("role")]
        public string Role { get; set; }
        
        [JsonPropertyName("content")]
        public string Content { get; set; }
        
        public OpenAIMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }
}
