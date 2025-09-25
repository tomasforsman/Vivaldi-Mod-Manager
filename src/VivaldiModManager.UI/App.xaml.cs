using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;
using VivaldiModManager.Core.Services;
using VivaldiModManager.UI.Services;
using VivaldiModManager.UI.ViewModels;
using VivaldiModManager.UI.Views;

namespace VivaldiModManager.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost _host;

    public App()
    {
        _host = CreateHostBuilder().Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
        }

        base.OnExit(e);
    }

    private static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register Core services
                services.AddTransient<IVivaldiService, VivaldiService>();
                services.AddTransient<IInjectionService, InjectionService>();
                services.AddTransient<IManifestService, ManifestService>();
                services.AddTransient<ILoaderService, LoaderService>();
                services.AddTransient<IHashService, HashService>();

                // Register UI services
                services.AddTransient<IDialogService, DialogService>();
                services.AddSingleton<ISystemTrayService, SystemTrayService>();
                services.AddSingleton<IThemeService, ThemeService>();

                // Register ViewModels
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<SettingsWindowViewModel>();

                // Register Views
                services.AddTransient<MainWindow>();
                services.AddTransient<SettingsWindow>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });
    }
}