namespace AIStorm.Core.AI;

public class OpenAIOptions
{
    public required string ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
}
