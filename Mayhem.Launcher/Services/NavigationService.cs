using CryptoMayhemLauncher.Interfaces;
using Mayhem.Launcher;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace CryptoMayhemLauncher.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider serviceProvider;

        public NavigationService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public Window Show<T>() where T : Window
        {
            bool isWindowOpen = false;
            Window result = null;
            foreach (Window w in Application.Current.Windows)
            {
                if (w is T)
                {
                    isWindowOpen = true;
                    w.Activate();
                    w.Show();

                    if (w is LoginWindow)
                    {
                        ((LoginWindow)w).UpdateLocalization();
                    }
                    else if (w is MainWindow)
                    {
                        ((MainWindow)w).UpdateLocalization();
                    }

                    result = w;
                }
            }

            if (!isWindowOpen)
            {
                Window newWindow = serviceProvider.GetService<T>();
                newWindow.Show();
                result = newWindow;
            }
            return result;
        }

        public void Close<T>() where T : Window
        {
            Window window = serviceProvider.GetService<T>();
            window.Close();
        }

        public void Hide<T>() where T : Window
        {
            Window window = serviceProvider.GetService<T>();
            window.Hide();
        }
    }
}
