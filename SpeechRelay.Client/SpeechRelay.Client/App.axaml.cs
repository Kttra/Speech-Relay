using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpeechRelay.Client.ViewModels;
using SpeechRelay.Client.Views;
using System;
using System.IO;
using System.Net.Http;

namespace SpeechRelay.Client
{
    public partial class App : Application
    {
        public static IServiceProvider? Services { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Load appsettings.json
            //var config = new ConfigurationBuilder()
            //    .SetBasePath(AppContext.BaseDirectory)
            //    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            //    .Build();
            var assembly = typeof(App).Assembly;
            using var stream = assembly.GetManifestResourceStream("SpeechRelay.Client.appsettings.json") ?? new MemoryStream();
            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
            var services = new ServiceCollection();

            // Register config as a simple object
            services.AddSingleton<IConfiguration>(config);

            // Register HttpClient with X-Auth-Token from config
            services.AddSingleton<HttpClient>(provider =>
            {
                var cfg = provider.GetRequiredService<IConfiguration>();
                var token = cfg["Api:AuthToken"];

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-Auth-Token", token);
                client.Timeout = TimeSpan.FromSeconds(10);
                return client;
            });

            // Register services
            services.AddSingleton<Services.SettingsService>();

            // Register ViewModels
            services.AddTransient<MainViewModel>();
            Services = services.BuildServiceProvider();
            var vm = Services.GetRequiredService<MainViewModel>();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = vm
                };
            }
            else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
            {
                singleViewFactoryApplicationLifetime.MainViewFactory = () => new MainView { DataContext = vm };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = vm
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}