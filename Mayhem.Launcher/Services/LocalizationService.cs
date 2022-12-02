using CryptoMayhemLauncher.Interfaces;
using Mayhem.Launcher.Models;
using System;
using System.Threading;
using System.Windows;

namespace CryptoMayhemLauncher.Services
{
    public class LocalizationService : ILocalizationService
    {
        private readonly ISettingsFileService settingsFileService;

        public LocalizationService(ISettingsFileService settingsFileService)
        {
            this.settingsFileService = settingsFileService;
        }

        public void SetLocalization(Window window)
        {
            ResourceDictionary resourceDictionary = new ResourceDictionary();
            resourceDictionary.Source = new Uri($"..\\StringResources.{GetDefaultLanguage()}.xaml", UriKind.Relative);
            window.Resources.MergedDictionaries.Add(resourceDictionary);
        }

        public string GetDefaultLanguage()
        {
            FileSettings fileSettings = settingsFileService.GetContent();

            if (fileSettings.CurrentCulture == "pl")
            {
                return "pl";
            }
            else if (fileSettings.CurrentCulture != string.Empty)
            {
                return "en";
            }
            else if (Thread.CurrentThread.CurrentCulture.Name == "pl")
            {
                return "pl";
            }
            else
            {
                return "en";
            }
        }
    }
}
