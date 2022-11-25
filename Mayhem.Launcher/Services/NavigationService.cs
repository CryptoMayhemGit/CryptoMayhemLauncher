using CryptoMayhemLauncher.Interfaces;
using System.Threading.Tasks;
using System.Windows;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoMayhemLauncher.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider serviceProvider;

        public NavigationService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void Hide<T>() where T : Window
        {
            var window = serviceProvider.GetRequiredService<T>();
            window.Close();
            window.Hide();
        }

        public T Show<T>() where T : Window
        {
            var window = serviceProvider.GetRequiredService<T>();
            window.Show();
            return window;
            //MainWindow windowToRun = serviceProvider.GetRequiredService<MainWindow>();
            //refInstance.Run(windowToRun);  
        }

        public async Task ShowAsync<T>(object parameter = null) where T : Window
        {
            var window = serviceProvider.GetRequiredService<T>();
            if (window is IActivable activableWindow)
            {
                await activableWindow.ActivateAsync(parameter);
            }

            window.Show();
            //MainWindow windowToRun = serviceProvider.GetRequiredService<MainWindow>();
            //refInstance.Run(windowToRun);
        }

        public async Task<bool?> ShowDialogAsync<T>(object parameter = null)
            where T : Window
        {
            var window = serviceProvider.GetRequiredService<T>();
            if (window is IActivable activableWindow)
            {
                await activableWindow.ActivateAsync(parameter);
            }

            return window.ShowDialog();
        }
    }
}
