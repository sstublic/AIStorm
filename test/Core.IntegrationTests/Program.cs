using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Extensions.Logging;
using System.IO;
using AIStorm.Core.Storage.Markdown;
using AIStorm.Core.Common;

namespace AIStorm.Core.IntegrationTests;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            // Check if we should only run Gemini tests
            bool runOnlyGeminiTests = args.Contains("gemini");
            
            // Set up the host with configuration
            using var host = CreateHostBuilder(args).Build();
            
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            
            if (runOnlyGeminiTests)
            {
                // Run only Gemini tests
                logger.LogInformation("=== Running Gemini Tests Only ===");
                var geminiTests = host.Services.GetRequiredService<GeminiTests>();
                await geminiTests.RunTest();
            }
            else
            {
                // Run all tests
                logger.LogInformation("=== Running OpenAI Tests ===");
                var openAITests = host.Services.GetRequiredService<OpenAITests>();
                await openAITests.RunAllTests();
                
                logger.LogInformation("=== Running Session Integration Tests ===");
                var sessionTests = host.Services.GetRequiredService<SessionIntegrationTests>();
                await sessionTests.RunTest();
                
                logger.LogInformation("=== Running Gemini Tests ===");
                var geminiTests = host.Services.GetRequiredService<GeminiTests>();
                await geminiTests.RunTest();
            }
            
            logger.LogInformation("All tests completed!");
        }
        finally
        {
            // Ensure to flush and stop NLog
            LogManager.Shutdown();
        }
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
                logging.AddNLog();
            })
            .ConfigureServices((context, services) =>
            {
                // Add AIStorm Core services
                services.AddAIStormCore(context.Configuration);
                
                // Configure the storage provider to use the test data directory
                // Using AppDomain.CurrentDomain.BaseDirectory to get the executable directory
                string testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
                
                // Add diagnostic logging to help troubleshoot path issues
                var startupLogger = LogManager.GetLogger("Startup");
                startupLogger.Info($"Integration Test Data Path: {testDataPath}");
                if (Directory.Exists(testDataPath))
                {
                    startupLogger.Info("TestData directory exists");
                    var files = Directory.GetFiles(testDataPath, "*.*", SearchOption.AllDirectories);
                    startupLogger.Info($"Files found: {files.Length}");
                    foreach (var file in files.Take(5)) // Show first 5 files to avoid too much output
                    {
                        startupLogger.Info($"  - {file}");
                    }
                }
                else
                {
                    startupLogger.Error("TestData directory does not exist!");
                }
                
                services.Configure<MarkdownStorageOptions>(options => 
                {
                    options.BasePath = testDataPath;
                });
                
                // Register the test classes
                services.AddSingleton<OpenAITests>();
                services.AddSingleton<SessionIntegrationTests>();
                services.AddSingleton<GeminiTests>();
            });
}
