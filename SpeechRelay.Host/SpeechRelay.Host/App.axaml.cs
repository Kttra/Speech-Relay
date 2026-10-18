using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpeechRelay.Host.Services;
using SpeechRelay.Host.ViewModels;
using SpeechRelay.Host.Views;
using System;

namespace SpeechRelay.Host;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 1. Load configuration
        using var stream = typeof(App).Assembly
            .GetManifestResourceStream("SpeechRelay.Host.appsettings.json")
            ?? throw new InvalidOperationException("Embedded configuration not found.");

        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        // 2. Register services
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IWebServer, WebServer>();
        services.AddSingleton<IPairingQrCodeService, PairingQrCodeService>();
        services.AddSingleton<MainWindowViewModel>();

        Services = services.BuildServiceProvider();

        // 3. Resolve ViewModel + WebServer
        var vm = Services.GetRequiredService<MainWindowViewModel>();
        var server = Services.GetRequiredService<IWebServer>();
        server.Start();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
