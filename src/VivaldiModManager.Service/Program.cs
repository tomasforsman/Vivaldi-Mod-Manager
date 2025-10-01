using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using VivaldiModManager.Core.Services;
using VivaldiModManager.Service.BackgroundServices;
using VivaldiModManager.Service.Configuration;
using VivaldiModManager.Service.Services;

namespace VivaldiModManager.Service;

/// <summary>
/// Main entry point for the Vivaldi Mod Manager Windows Service.
/// </summary>
public class Program
{
    /// <summary>
    /// Entry point for the application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync().ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    /// <summary>
    /// Creates the host builder for the Windows Service.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>The configured host builder.</returns>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseWindowsService(options =>
            {
                options.ServiceName = "VivaldiModManagerService";
            })
            .ConfigureServices((hostContext, services) =>
            {
                // Load service configuration
                var serviceConfig = ServiceConfiguration.LoadFromConfiguration(hostContext.Configuration);
                services.AddSingleton(serviceConfig);

                // Register Core services as singletons
                services.AddSingleton<IManifestService, ManifestService>();
                services.AddSingleton<IVivaldiService, VivaldiService>();
                services.AddSingleton<IInjectionService, InjectionService>();
                services.AddSingleton<ILoaderService, LoaderService>();
                services.AddSingleton<IHashService, HashService>();

                // Register background services as both singleton and hosted service
                services.AddSingleton<FileSystemMonitorService>();
                services.AddHostedService(provider => provider.GetRequiredService<FileSystemMonitorService>());

                services.AddSingleton<IntegrityCheckService>();
                services.AddHostedService(provider => provider.GetRequiredService<IntegrityCheckService>());

                // Register IPC server as both singleton and hosted service
                services.AddSingleton<IPCServerService>();
                services.AddHostedService(provider => provider.GetRequiredService<IPCServerService>());
            })
            .ConfigureLogging((hostContext, logging) =>
            {
                logging.ClearProviders();
                
                // Console logging for debugging
                logging.AddConsole();

                // Windows Event Log for production
                logging.AddEventLog(new EventLogSettings
                {
                    SourceName = "VivaldiModManagerService",
                    LogName = "Application"
                });

                // Set log levels from configuration
                logging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
            });
}
