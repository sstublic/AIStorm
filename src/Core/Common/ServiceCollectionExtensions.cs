namespace AIStorm.Core.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using AIStorm.Core.AI;
using AIStorm.Core.Storage;
using AIStorm.Core.Storage.Markdown;
using AIStorm.Core.SessionManagement;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAIStormCore(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure AI options
        services.Configure<OpenAIOptions>(
            configuration.GetSection($"AI:{OpenAIOptions.ProviderName}"));
            
        services.Configure<AIMockOptions>(
            configuration.GetSection($"AI:{AIMockOptions.ProviderName}"));
        
        services.Configure<MarkdownStorageOptions>(
            configuration.GetSection(nameof(MarkdownStorageOptions)));
        
        // Register services
        services.AddSingleton<MarkdownSerializer>();
        services.AddSingleton<IStorageProvider, MarkdownStorageProvider>();
        services.AddSingleton<IPromptBuilder, PromptBuilder>();
        services.AddSingleton<IAIProvider, OpenAIProvider>();
        services.AddSingleton<IAIProvider, AIMockProvider>();
        services.AddSingleton<ISessionRunnerFactory, SessionRunnerFactory>();
        services.AddSingleton<AIProviderManager>();
        
        // Add logging (already provided by default through DI in most .NET apps)
        services.AddLogging();
        
        return services;
    }
}
