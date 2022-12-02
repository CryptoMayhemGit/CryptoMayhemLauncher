using System.Windows;

namespace CryptoMayhemLauncher.Interfaces
{
    public interface ILocalizationService
    {
        void SetLocalization(Window window);
        string GetDefaultLanguage();
    }
}
