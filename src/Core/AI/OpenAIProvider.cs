namespace AIStorm.Core.AI;

using AIStorm.Core.Models;
using AIStorm.Core.SessionManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class OpenAIProvider : IAIProvider
{
    private const string BASE_URL = "https://api.openai.com/v1/";
    
    private readonly HttpClient httpClient;
    private readonly ILogger<OpenAIProvider> logger;
    private readonly IPromptBuilder promptBuilder;
    private readonly OpenAIOptions options;
    
    public OpenAIProvider(
        IOptions<OpenAIOptions> options, 
        ILogger<OpenAIProvider> logger,
        IPromptBuilder promptBuilder)
    {
        this.logger = logger;
        this.promptBuilder = promptBuilder;
        this.options = options.Value;
        
        logger.LogInformation("Initializing OpenAIProvider");
        
        // Validate options
        if (string.IsNullOrEmpty(this.options.ApiKey))
        {
            logger.LogWarning("OpenAI API key is missing - provider may not work correctly");
        }
            
        this.httpClient = new HttpClient
        {
            BaseAddress = new Uri(BASE_URL)
        };
        
        if (!string.IsNullOrEmpty(this.options.ApiKey))
        {
            this.httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {this.options.ApiKey}");
        }
    }
    
    public async Task<string> SendMessageAsync(Agent agent, SessionPremise premise, List<StormMessage> conversationHistory)
    {
        try
        {
            logger.LogDebug("Sending message to OpenAI for agent: {AgentName} using model: {Model}", 
                agent.Name, agent.AIModel);
            
            var promptMessages = promptBuilder.BuildPrompt(agent, premise, conversationHistory);
            var messages = promptMessages.Select(m => new OpenAIMessage(m.Role, m.Content)).ToList();
            
            var requestData = new
            {
                model = agent.AIModel,
                messages,
                temperature = 0.7f
            };
            
            var requestJson = JsonSerializer.Serialize(requestData, new JsonSerializerOptions { WriteIndented = true });
            logger.LogDebug("OpenAI request payload: {RequestJson}", requestJson);
            
            var content = new StringContent(
                requestJson, 
                Encoding.UTF8, 
                "application/json");
                
            var response = await httpClient.PostAsync("chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("OpenAI API error: Status code {StatusCode}, Details: {ErrorContent}", 
                    (int)response.StatusCode, errorContent);
                    
                throw new HttpRequestException(
                    $"OpenAI API returned {(int)response.StatusCode} ({response.StatusCode}). " +
                    $"Details: {errorContent}. " +
                    "Please check your API key and configuration."
                );
            }
        
            var responseContent = await response.Content.ReadAsStringAsync();
            logger.LogDebug("OpenAI response: {ResponseContent}", responseContent);
            
            var responseJson = JsonDocument.Parse(responseContent);
            
            var responseText = responseJson.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
                
            logger.LogDebug("Received response from OpenAI, length: {Length} characters", 
                responseText?.Length ?? 0);
                
            var cleanedResponse = PromptTools.RemoveAgentNamePrefixFromMessage(responseText ?? string.Empty);
                
            return cleanedResponse;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error communicating with OpenAI API");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error communicating with OpenAI API");
            throw new Exception($"Error communicating with OpenAI API: {ex.Message}", ex);
        }
    }
    
    public Task<string[]> GetAvailableModelsAsync()
    {
        logger.LogTrace("Returning {Count} models from configuration", options.Models.Count);
        return Task.FromResult(options.Models.ToArray());
    }

    public string GetProviderName() => OpenAIOptions.ProviderName;
    
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
