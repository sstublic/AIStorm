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
            
            // Create the request object
            var request = new AnthropicRequest
            {
                Model = agent.AIModel,
                System = systemMessage,
                Messages = messages,
                MaxTokens = 8000,
                Temperature = 0.7f
            };
            
            // Add thinking capability for Claude 3.7 models
            if (IsClaude37Model(agent.AIModel))
            {
                request.EnableThinking();
            }
            
            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
            logger.LogDebug("Anthropic request payload: {RequestJson}", requestJson);
            
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                
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
            
            // Parse the response
            var anthropicResponse = JsonSerializer.Deserialize<AnthropicResponse>(responseContent);
            string responseText = string.Empty;
            
            try
            {
                if (anthropicResponse?.Content != null)
                {
                    // When thinking is enabled, response has multiple content items with different types
                    // Find the "text" type content which contains the actual response
                    var textContent = anthropicResponse.Content.FirstOrDefault(c => c.Type == "text");
                    if (textContent != null && !string.IsNullOrEmpty(textContent.Text))
                    {
                        responseText = textContent.Text;
                        
                        // Log thinking content if available (useful for debugging)
                        var thinkingContent = anthropicResponse.Content.FirstOrDefault(c => c.Type == "thinking");
                        if (thinkingContent != null && !string.IsNullOrEmpty(thinkingContent.Thinking))
                        {
                            logger.LogDebug("Thinking content: {Thinking}", 
                                thinkingContent.Thinking.Length > 100 
                                    ? thinkingContent.Thinking.Substring(0, 100) + "..." 
                                    : thinkingContent.Thinking);
                        }
                    }
                    else
                    {
                        logger.LogWarning("No text content found in Anthropic response");
                    }
                }
                else
                {
                    logger.LogWarning("Anthropic response doesn't contain expected 'content' array or it's empty");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error parsing Anthropic response: {Response}", responseContent);
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
    
    private bool IsClaude37Model(string modelName)
    {
        return modelName.Contains("claude-3-7", StringComparison.OrdinalIgnoreCase);
    }
    
    private class AnthropicThinking
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "enabled";
        
        [JsonPropertyName("budget_tokens")]
        public int BudgetTokens { get; set; } = 4000;
    }
    
    private class AnthropicRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; set; }
        
        [JsonPropertyName("system")]
        public required string System { get; set; }
        
        [JsonPropertyName("messages")]
        public required List<AnthropicMessage> Messages { get; set; }
        
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 8000;
        
        [JsonPropertyName("temperature")]
        public float Temperature { get; set; } = 0.7f;
        
        [JsonPropertyName("thinking")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public AnthropicThinking? Thinking { get; set; }
        
        // Important: When thinking is enabled, temperature must be set to 1
        public void EnableThinking()
        {
            Thinking = new AnthropicThinking();
            Temperature = 1.0f; // Required when thinking is enabled
        }
    }
    
    private class AnthropicResponse
    {
        [JsonPropertyName("content")]
        public required List<AnthropicContentItem> Content { get; set; }
    }
    
    private class AnthropicContentItem
    {
        [JsonPropertyName("type")]
        public required string Type { get; set; }
        
        [JsonPropertyName("text")]
        public string? Text { get; set; }
        
        [JsonPropertyName("thinking")]
        public string? Thinking { get; set; }
    }
    
    private class AnthropicMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;
        
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
        
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
