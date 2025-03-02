namespace AIStorm.Core.Services;

using AIStorm.Core.Models;
using AIStorm.Core.Services.Options;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<OpenAIProvider> logger;
    
    public OpenAIProvider(IOptions<OpenAIOptions> options, ILogger<OpenAIProvider> logger)
    {
        this.logger = logger;
        var openAIOptions = options.Value;
        
        logger.LogInformation("Initializing OpenAIProvider with base URL: {BaseUrl}", openAIOptions.BaseUrl);
        
        // Validate options
        if (string.IsNullOrEmpty(openAIOptions.ApiKey))
        {
            logger.LogError("OpenAI API key is missing");
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
            logger.LogInformation("Sending message to OpenAI for agent: {AgentName} using model: {Model}", 
                agent.Name, agent.AIModel);
            
            // Format conversation history according to OpenAI's API expectations
            var messages = FormatConversationForAgent(agent, conversationHistory, userMessage);
            
            // Create the request JSON directly
            var requestData = new
            {
                model = agent.AIModel,
                messages = messages,
                temperature = 0.7f
            };
            
            var requestJson = JsonSerializer.Serialize(requestData);
            logger.LogDebug("OpenAI request payload: {RequestJson}", requestJson);
            
            var content = new StringContent(
                requestJson, 
                Encoding.UTF8, 
                "application/json");
                
            var response = await httpClient.PostAsync("chat/completions", content);
            
            // If the request fails, try to get more details about the error
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
            
            // Parse response and extract the generated text
            var responseText = responseJson.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
                
            logger.LogInformation("Received response from OpenAI, length: {Length} characters", 
                responseText?.Length ?? 0);
                
            // Clean up any existing prefixes from the response
            var cleanedResponse = PromptTools.CleanupResponse(responseText ?? string.Empty);
                
            // Prepend the agent name to the response
            string formattedResponse = $"[{agent.Name}]: {cleanedResponse}";
            
            return formattedResponse;
        }
        catch (HttpRequestException ex)
        {
            // Log the error and pass through our custom error message
            logger.LogError(ex, "HTTP error communicating with OpenAI API");
            throw;
        }
        catch (Exception ex)
        {
            // Wrap other exceptions for more context
            logger.LogError(ex, "Error communicating with OpenAI API");
            throw new Exception($"Error communicating with OpenAI API: {ex.Message}", ex);
        }
    }
    
    public async Task<string[]> GetAvailableModelsAsync()
    {
        try
        {
            logger.LogInformation("Retrieving available models from OpenAI");
            
            var response = await httpClient.GetAsync("models");
            
            // If the request fails, try to get more details about the error
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Failed to retrieve models: Status code {StatusCode}, Details: {ErrorContent}", 
                    (int)response.StatusCode, errorContent);
                    
                throw new HttpRequestException(
                    $"OpenAI API returned {(int)response.StatusCode} ({response.StatusCode}). " +
                    $"Details: {errorContent}. " +
                    "Please check your API key and configuration."
                );
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            logger.LogDebug("OpenAI models response: {ResponseContent}", responseContent);
            
            var responseJson = JsonDocument.Parse(responseContent);
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
            
            logger.LogInformation("Retrieved {Count} GPT models from OpenAI", models.Count);
            return models.ToArray();
        }
        catch (HttpRequestException ex)
        {
            // Log the error and pass through our custom error message
            logger.LogError(ex, "HTTP error retrieving models from OpenAI API");
            throw;
        }
        catch (Exception ex)
        {
            // Wrap other exceptions for more context
            logger.LogError(ex, "Error retrieving models from OpenAI API: {Message}", ex.Message);
            throw new Exception($"Error retrieving models from OpenAI API: {ex.Message}", ex);
        }
    }
    
    private List<OpenAIMessage> FormatConversationForAgent(Agent agent, List<StormMessage> conversationHistory, string userMessage)
    {
        string enhancedSystemPrompt = PromptTools.CreateExtendedSystemPrompt(agent);
            
        var messages = new List<OpenAIMessage>
        {
            new OpenAIMessage("system", enhancedSystemPrompt)
        };
        
        // Add conversation history
        foreach (var message in conversationHistory)
        {
            // Determine role based on the agent name
            string role = message.AgentName == agent.Name ? "assistant" : "user";
            // The message content already includes the agent name prefix, so we don't need to add it again
            messages.Add(new OpenAIMessage(role, message.Content));
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
