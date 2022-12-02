using CryptoMayhemLauncher.Interfaces;
using CryptoMayhemLauncher.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;

namespace Mayhem.Launcher
{
    public partial class App : Application
    {
        private readonly IHost host;
        private readonly ILogger<App> logger;
        private readonly ISettingsFileService settingsFileService;
        private readonly INavigationService navigationService;
        private readonly ISqurrielHandleEvents squrrielHandleEvents;
        //private readonly ILocalizationService localizationService;

        public App(string[] args)
        {
            host = new HostBuilder()
                    .ConfigureServices((hostContext, services) =>
                    {
                        ConfigureServices(services);
                        services.BuildServiceProvider();

                    }).ConfigureLogging(logBuilder =>
                    {
                        logBuilder.SetMinimumLevel(LogLevel.Trace)
                                  .AddLog4Net();

                    }).Build();

            logger = (ILogger<App>)host.Services.GetService(typeof(ILogger<App>));
            settingsFileService = (ISettingsFileService)host.Services.GetService(typeof(ISettingsFileService));
            navigationService = (INavigationService)host.Services.GetService(typeof(INavigationService));
            squrrielHandleEvents = (ISqurrielHandleEvents)host.Services.GetService(typeof(ISqurrielHandleEvents));
            // = (ILocalizationService)host.Services.GetService(typeof(ILocalizationService));
            logger.LogWarning(string.Join(",", args));
            Init();
        }

        private void ConfigureServices(IServiceCollection services)
            {
                services.AddScoped<INavigationService, NavigationService>();
                services.AddScoped<ISettingsFileService, SettingsFileService>();
                services.AddScoped<IVersionService, VersionService>();
                services.AddScoped<ISqurrielHandleEvents, SqurrielHandleEvents>();
                services.AddScoped<ILocalizationService, LocalizationService>();
                services.AddSingleton<MainWindow>();
                services.AddSingleton<PathSettings>();
                services.AddSingleton<LoginWindow>();
                services.AddHttpClient();
            }

            private void Init()
            {
            /*foreach (string item in e.Args)
            {
                logger.LogInformation($"Params 123 => {item}");
            }*/
            SingleApplicationCheck();
            squrrielHandleEvents.SetDefaultConfiguration();

            /*foreach (string item in e.Args)
            {
                logger.LogInformation($"Params {item}");
            }*/

            //navigationService.Show<MainWindow>();
            //navigationService.Hide<MainWindow>();
            Window windows = null;
            if (IsFirstRun())
            {
                logger.LogInformation("Nie Istnieje!");
                    settingsFileService.TryCreate();
                windows = navigationService.Show<PathSettings>();
                }
                else
            {
                logger.LogInformation("Istnieje!");
                    if (!settingsFileService.IsFileExist())
                    {
                        logger.LogCritical("Configuration file not exist.");
                        Application.Current.Shutdown();
                    }
                    else if (!settingsFileService.IsGamePathExist())
                    {
                        logger.LogCritical("The path to the game folder has not been set correctly.");
                        Application.Current.Shutdown();
                    }
                windows = navigationService.Show<LoginWindow>();
            }

           settingsFileService.UpdateWallet(string.Empty);
            Run(windows);
        }

            private void SingleApplicationCheck()
            {
                Process process = Process.GetCurrentProcess();
                int processCount = Process.GetProcesses().Where(p =>
                    p.ProcessName == process.ProcessName).Count();

                if (processCount > 1)
                {
                    logger.LogInformation($"Already an instance is running...");
                    Application.Current.Shutdown();
                }
            }

            private bool IsFirstRun()
            {
                return !settingsFileService.IsFileExist()
                    || !settingsFileService.IsGamePathExist();
            }
        }
}
