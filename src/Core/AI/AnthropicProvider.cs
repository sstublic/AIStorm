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

public class AnthropicProvider : IAIProvider
{
    private const string BASE_URL = "https://api.anthropic.com/v1/";
    private const string API_VERSION = "2023-06-01"; // Update as needed
    
    private readonly HttpClient httpClient;
    private readonly ILogger<AnthropicProvider> logger;
    private readonly IPromptBuilder promptBuilder;
    private readonly AnthropicOptions options;
    
    public AnthropicProvider(
        IOptions<AnthropicOptions> options, 
        ILogger<AnthropicProvider> logger,
        IPromptBuilder promptBuilder)
    {
        this.logger = logger;
        this.promptBuilder = promptBuilder;
        this.options = options.Value;
        
        logger.LogInformation("Initializing AnthropicProvider");
        
        // Validate options
        if (string.IsNullOrEmpty(this.options.ApiKey))
        {
            logger.LogWarning("Anthropic API key is missing - provider may not work correctly");
        }
            
        this.httpClient = new HttpClient
        {
            BaseAddress = new Uri(BASE_URL)
        };
        
        if (!string.IsNullOrEmpty(this.options.ApiKey))
        {
            this.httpClient.DefaultRequestHeaders.Add("x-api-key", this.options.ApiKey);
            this.httpClient.DefaultRequestHeaders.Add("anthropic-version", API_VERSION);
        }
    }
    
    public async Task<string> SendMessageAsync(Agent agent, SessionPremise premise, List<StormMessage> conversationHistory)
    {
        try
        {
            logger.LogDebug("Sending message to Anthropic for agent: {AgentName} using model: {Model}", 
                agent.Name, agent.AIModel);
            
            var promptMessages = promptBuilder.BuildPrompt(agent, premise, conversationHistory);
            
            // Extract system message (first message should be the system message)
            string systemMessage = promptMessages.FirstOrDefault(m => m.Role.ToLower() == "system")?.Content ?? string.Empty;
            
            // Filter to only include user and assistant messages for the messages array
            // PromptBuilder ensures we always have at least one user message
            var messages = promptMessages
                .Where(m => m.Role.ToLower() != "system")
                .Select(m => new AnthropicMessage(m.Role, m.Content))
                .ToList();
            
            var requestData = new
            {
                model = agent.AIModel,
                system = systemMessage,
                messages,
                max_tokens = 4000,
                temperature = 0.7f
            };
            
            var requestJson = JsonSerializer.Serialize(requestData, new JsonSerializerOptions { WriteIndented = true });
            logger.LogDebug("Anthropic request payload: {RequestJson}", requestJson);
            
            var content = new StringContent(
                requestJson, 
                Encoding.UTF8, 
                "application/json");
                
            var response = await httpClient.PostAsync("messages", content);
            
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Anthropic API error: Status code {StatusCode}, Details: {ErrorContent}", 
                    (int)response.StatusCode, errorContent);
                    
                throw new HttpRequestException(
                    $"Anthropic API returned {(int)response.StatusCode} ({response.StatusCode}). " +
                    $"Details: {errorContent}. " +
                    "Please check your API key and configuration."
                );
            }
        
            var responseContent = await response.Content.ReadAsStringAsync();
            logger.LogDebug("Anthropic response: {ResponseContent}", responseContent);
            
            var responseJson = JsonDocument.Parse(responseContent);
            
            string responseText = string.Empty;
            try
            {
                // Check if the response has a content property that's an array
                if (responseJson.RootElement.TryGetProperty("content", out var contentElement) && 
                    contentElement.ValueKind == JsonValueKind.Array && 
                    contentElement.GetArrayLength() > 0)
                {
                    // Get the first content element
                    var firstContent = contentElement[0];
                    
                    // Check if it has a text property
                    if (firstContent.TryGetProperty("text", out var textElement))
                    {
                        responseText = textElement.GetString() ?? string.Empty;
                    }
                    else
                    {
                        logger.LogWarning("Anthropic response doesn't contain expected 'text' property in the first content element");
                    }
                }
                else
                {
                    logger.LogWarning("Anthropic response doesn't contain expected 'content' array or it's empty");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error parsing Anthropic response JSON");
                throw new Exception($"Error parsing Anthropic response: {ex.Message}", ex);
            }
            
            if (string.IsNullOrEmpty(responseText))
            {
                logger.LogWarning("Empty response text from Anthropic API");
                responseText = "No response content received from the AI service.";
            }
            
            logger.LogDebug("Received response from Anthropic, length: {Length} characters", responseText.Length);
                
            var cleanedResponse = PromptTools.RemoveAgentNamePrefixFromMessage(responseText);
                
            return cleanedResponse;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error communicating with Anthropic API");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error communicating with Anthropic API");
            throw new Exception($"Error communicating with Anthropic API: {ex.Message}", ex);
        }
    }
    
    public Task<string[]> GetAvailableModelsAsync()
    {
        logger.LogTrace("Returning {Count} models from configuration", options.Models.Count);
        return Task.FromResult(options.Models.ToArray());
    }

    public string GetProviderName() => AnthropicOptions.ProviderName;
    
    private class AnthropicMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }
        
        [JsonPropertyName("content")]
        public string Content { get; set; }
        
        public AnthropicMessage(string role, string content)
        {
            // Map OpenAI roles to Anthropic roles
            Role = role.ToLower() switch
            {
                "assistant" => "assistant",
                _ => "user"  // Default to user for any other role
            };
            
            Content = content;
        }
    }
}
