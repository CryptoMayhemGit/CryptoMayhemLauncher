using Mayhem.Dal.Tables;
using System.Threading.Tasks;

namespace CryptoMayhemLauncher.Interfaces
{
    public interface ILivepeerService
    {
        string GetNextAssetUrl();
        Task Init();
    }
}
