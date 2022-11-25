using System.Threading.Tasks;

namespace CryptoMayhemLauncher.Interfaces
{
    public interface IActivable
    {
        Task ActivateAsync(object parameter);
    }
}
