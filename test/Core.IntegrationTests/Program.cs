using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AIStorm.Core.Services;
using AIStorm.Core.Services.Options;
using Microsoft.Extensions.Options;
using System.IO;

namespace AIStorm.Core.IntegrationTests;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Set up the host with configuration
        using var host = CreateHostBuilder(args).Build();
        
        // Run the OpenAI tests
        Console.WriteLine("=== Running OpenAI Tests ===\n");
        var openAITests = host.Services.GetRequiredService<OpenAITests>();
        await openAITests.RunAllTests();
        
        Console.WriteLine("\n=== Running Session Integration Tests ===\n");
        var sessionTests = host.Services.GetRequiredService<SessionIntegrationTests>();
        await sessionTests.RunTest();
        
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
                
                // Configure the storage provider to use the test data directory
                // Using AppDomain.CurrentDomain.BaseDirectory to get the executable directory
                string testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
                
                // Add diagnostic logging to help troubleshoot path issues
                Console.WriteLine($"Integration Test Data Path: {testDataPath}");
                if (Directory.Exists(testDataPath))
                {
                    Console.WriteLine("TestData directory exists");
                    var files = Directory.GetFiles(testDataPath, "*.*", SearchOption.AllDirectories);
                    Console.WriteLine($"Files found: {files.Length}");
                    foreach (var file in files.Take(5)) // Show first 5 files to avoid too much output
                    {
                        Console.WriteLine($"  - {file}");
                    }
                }
                else
                {
                    Console.WriteLine("TestData directory does not exist!");
                }
                
                services.Configure<MarkdownStorageOptions>(options => 
                {
                    options.BasePath = testDataPath;
                });
                
                // Register the test classes
                services.AddSingleton<OpenAITests>();
                services.AddSingleton<SessionIntegrationTests>();
            });
}
