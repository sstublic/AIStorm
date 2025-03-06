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

public class GeminiProvider : IAIProvider
{
    private const string BASE_URL = "https://generativelanguage.googleapis.com/v1/";
    
    private readonly HttpClient httpClient;
    private readonly ILogger<GeminiProvider> logger;
    private readonly IPromptBuilder promptBuilder;
    private readonly GeminiOptions options;
    
    public GeminiProvider(
        IOptions<GeminiOptions> options, 
        ILogger<GeminiProvider> logger,
        IPromptBuilder promptBuilder)
    {
        this.logger = logger;
        this.promptBuilder = promptBuilder;
        this.options = options.Value;
        
        logger.LogInformation("Initializing GeminiProvider");
        
        // Validate options
        if (string.IsNullOrEmpty(this.options.ApiKey))
        {
            logger.LogWarning("Gemini API key is missing - provider may not work correctly");
        }
            
        this.httpClient = new HttpClient
        {
            BaseAddress = new Uri(BASE_URL)
        };
    }
    
    public async Task<string> SendMessageAsync(Agent agent, SessionPremise premise, List<StormMessage> conversationHistory)
    {
        try
        {
            logger.LogDebug("Sending message to Gemini for agent: {AgentName} using model: {Model}", 
                agent.Name, agent.AIModel);
            
            var promptMessages = promptBuilder.BuildPrompt(agent, premise, conversationHistory);
            var contents = new List<GeminiMessage>();
            
            // Handle system message separately as Gemini doesn't have a system role
            var systemMessage = promptMessages.FirstOrDefault(m => m.Role.Equals("system", StringComparison.OrdinalIgnoreCase));
            string systemContent = systemMessage?.Content ?? "";
            
            // Convert regular messages to Gemini format
            foreach (var message in promptMessages.Where(m => !m.Role.Equals("system", StringComparison.OrdinalIgnoreCase)))
            {
                // For the first user message, prepend system content if it exists
                if (contents.Count == 0 && 
                    message.Role.Equals("user", StringComparison.OrdinalIgnoreCase) && 
                    !string.IsNullOrEmpty(systemContent))
                {
                    contents.Add(new GeminiMessage(
                        "user", 
                        $"{systemContent}\n\n{message.Content}"
                    ));
                }
                else
                {
                    contents.Add(new GeminiMessage(message.Role, message.Content));
                }
            }
            
            var requestData = new
            {
                contents,
                generationConfig = new
                {
                    temperature = 0.7f
                }
            };
            
            var requestJson = JsonSerializer.Serialize(requestData, new JsonSerializerOptions { WriteIndented = true });
            logger.LogDebug("Gemini request payload: {RequestJson}", requestJson);
            
            // Add API key to query string
            string apiPath = $"models/{agent.AIModel}:generateContent?key={options.ApiKey}";
            
            var content = new StringContent(
                requestJson, 
                Encoding.UTF8, 
                "application/json");
                
            var response = await httpClient.PostAsync(apiPath, content);
            
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Gemini API error: Status code {StatusCode}, Details: {ErrorContent}", 
                    (int)response.StatusCode, errorContent);
                    
                throw new HttpRequestException(
                    $"Gemini API returned {(int)response.StatusCode} ({response.StatusCode}). " +
                    $"Details: {errorContent}. " +
                    "Please check your API key and configuration."
                );
            }
        
            var responseContent = await response.Content.ReadAsStringAsync();
            logger.LogDebug("Gemini response: {ResponseContent}", responseContent);
            
            var responseJson = JsonDocument.Parse(responseContent);
            
            var responseText = responseJson.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
                
            logger.LogDebug("Received response from Gemini, length: {Length} characters", 
                responseText?.Length ?? 0);
                
            var cleanedResponse = PromptTools.RemoveAgentNamePrefixFromMessage(responseText ?? string.Empty);
                
            return cleanedResponse;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error communicating with Gemini API");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error communicating with Gemini API");
            throw new Exception($"Error communicating with Gemini API: {ex.Message}", ex);
        }
    }
    
    public Task<string[]> GetAvailableModelsAsync()
    {
        logger.LogTrace("Returning {Count} models from configuration", options.Models.Count);
        return Task.FromResult(options.Models.ToArray());
    }

    public string GetProviderName() => GeminiOptions.ProviderName;
    
    private class GeminiMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }
        
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; }
        
        public GeminiMessage(string role, string content)
        {
            // Map OpenAI roles to Gemini roles
            Role = role.ToLower() switch
            {
                "assistant" => "model",
                _ => "user"
            };
            
            Parts = new List<Part> { new Part { Text = content } };
        }
        
        public class Part
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }
    }
}
