using Mayhem.Launcher;
using System.Threading.Tasks;
using System.Windows;

namespace CryptoMayhemLauncher.Interfaces
{
    public interface INavigationService
    {
        T Show<T>() where T : Window;
        void Hide<T>() where T : Window;
        Task ShowAsync<T>(object parameter = null) where T : Window;
        Task<bool?> ShowDialogAsync<T>(object parameter = null) where T : Window;
    }
}
