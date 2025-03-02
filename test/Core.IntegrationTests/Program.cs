using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AIStorm.Core.Services;

namespace AIStorm.Core.IntegrationTests;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Set up the host with configuration
        using var host = CreateHostBuilder(args).Build();
        
        // Run the tests
        var openAITests = host.Services.GetRequiredService<OpenAITests>();
        await openAITests.RunAllTests();
        
        Console.WriteLine("\nAll tests completed!");
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true)
                      .AddUserSecrets<Program>()
                      .AddEnvironmentVariables();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                logging.AddConsole();
            })
            .ConfigureServices((context, services) =>
            {
                // Add AIStorm Core services
                services.AddAIStormCore(context.Configuration);
                
                // Register the test class
                services.AddSingleton<OpenAITests>();
            });
}
