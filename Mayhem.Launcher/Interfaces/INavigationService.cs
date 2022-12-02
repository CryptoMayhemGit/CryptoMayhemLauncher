using System.Windows;

namespace CryptoMayhemLauncher.Interfaces
{
    public interface INavigationService
    {
        void Close<T>() where T : Window;
        void Hide<T>() where T : Window;
        Window Show<T>() where T : Window;
    }
}
