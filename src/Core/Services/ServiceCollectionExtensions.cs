namespace AIStorm.Core.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AIStorm.Core.Services.Options;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAIStormCore(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options using nameof() for section names
        services.Configure<OpenAIOptions>(
            configuration.GetSection(nameof(OpenAIOptions)));
        
        services.Configure<MarkdownStorageOptions>(
            configuration.GetSection(nameof(MarkdownStorageOptions)));
        
        // Register services
        services.AddSingleton<MarkdownSerializer>();
        services.AddSingleton<IStorageProvider, MarkdownStorageProvider>();
        services.AddSingleton<IAIProvider, OpenAIProvider>();
        
        return services;
    }
}
