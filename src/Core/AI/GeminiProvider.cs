namespace AIStorm.Core.AI;

using AIStorm.Core.Models;
using AIStorm.Core.SessionManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class GeminiProvider : IAIProvider
{
    private const string BASE_URL = "https://generativelanguage.googleapis.com/v1beta/";
    
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
            
        this.httpClient = new HttpClient();
        this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
    
    public async Task<string> SendMessageAsync(Agent agent, SessionPremise premise, List<StormMessage> conversationHistory)
    {
        try
        {
            logger.LogDebug("Sending message to Gemini for agent: {AgentName} using model: {Model}", 
                agent.Name, agent.AIModel);
            
            var promptMessages = promptBuilder.BuildPrompt(agent, premise, conversationHistory);
            
            // Handle system message separately as Gemini doesn't have a system role
            var systemMessage = promptMessages.FirstOrDefault(m => m.Role.Equals("system", StringComparison.OrdinalIgnoreCase));
            string systemContent = systemMessage?.Content ?? "";
            
            // Prepare message contents in new API format
            var contents = new List<GeminiContent>();
            
            // Convert regular messages to Gemini format
            foreach (var message in promptMessages.Where(m => !m.Role.Equals("system", StringComparison.OrdinalIgnoreCase)))
            {
                // Map roles correctly for Gemini v1beta API (only "user" and "model" are valid)
                string geminiRole = message.Role.ToLower() switch
                {
                    "assistant" => "model",
                    _ => "user"
                };
                
                // For the first user message, prepend system content if it exists
                if (contents.Count == 0 && 
                    message.Role.Equals("user", StringComparison.OrdinalIgnoreCase) && 
                    !string.IsNullOrEmpty(systemContent))
                {
                    contents.Add(new GeminiContent
                    {
                        role = geminiRole,
                        parts = new[] 
                        { 
                            new GeminiPart { text = $"{systemContent}\n\n{message.Content}" } 
                        }
                    });
                }
                else
                {
                    contents.Add(new GeminiContent
                    {
                        role = geminiRole,
                        parts = new[] 
                        { 
                            new GeminiPart { text = message.Content } 
                        }
                    });
                }
            }
            
            // Gemini API requires at least one user message
            if (contents.Count == 0)
            {
                logger.LogDebug("No non-system messages found, adding a default user message");
                
                // Add a default user message with the system content if it exists
                contents.Add(new GeminiContent
                {
                    role = "user",
                    parts = new[] 
                    { 
                        new GeminiPart { 
                            text = !string.IsNullOrEmpty(systemContent) 
                                ? $"{systemContent}\n\nBegin the conversation based on the provided context." 
                                : "Begin the conversation." 
                        } 
                    }
                });
            }
            
            var requestData = new
            {
                contents,
                generationConfig = new
                {
                    temperature = 0.7f
                }
            };
            
            var requestJson = JsonSerializer.Serialize(requestData);
            logger.LogDebug("Gemini request payload: {RequestJson}", requestJson);
            
            // Add API key to query string
            string apiUrl = $"{BASE_URL}models/{agent.AIModel}:generateContent?key={options.ApiKey}";
            
            var content = new StringContent(
                requestJson, 
                Encoding.UTF8, 
                "application/json");
                
            var response = await httpClient.PostAsync(apiUrl, content);
            
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
            
            // Deserialize response with proper classes
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
            
            if (geminiResponse?.candidates == null || geminiResponse.candidates.Length == 0 ||
                geminiResponse.candidates[0].content?.parts == null || geminiResponse.candidates[0].content.parts.Length == 0)
            {
                throw new Exception("Invalid or empty response from Gemini API");
            }
            
            var responseText = geminiResponse.candidates[0].content.parts[0].text;
            
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
    
    // Response models for the new API format
    private class GeminiResponse
    {
        public Candidate[] candidates { get; set; }
        public PromptFeedback promptFeedback { get; set; }
    }

    private class Candidate
    {
        public Content content { get; set; }
        public string finishReason { get; set; }
        public int index { get; set; }
        public SafetyRating[] safetyRatings { get; set; }
    }

    private class Content
    {
        public GeminiPart[] parts { get; set; }
    }

    private class GeminiContent
    {
        [JsonPropertyName("role")]
        public string role { get; set; }
        
        public GeminiPart[] parts { get; set; }
    }

    private class GeminiPart
    {
        public string text { get; set; }
    }

    private class PromptFeedback
    {
        public SafetyRating[] safetyRatings { get; set; }
        public string blockReason { get; set; }
    }

    private class SafetyRating
    {
        public string category { get; set; }
        public string probability { get; set; }
    }
}
