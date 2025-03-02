namespace AIStorm.Core.Services.Options;

public class OpenAIOptions
{
    public string ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
}
