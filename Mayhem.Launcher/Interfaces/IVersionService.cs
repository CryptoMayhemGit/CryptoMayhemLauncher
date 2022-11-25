using Mayhem.Dal.Tables;
using System.Threading.Tasks;

namespace CryptoMayhemLauncher.Interfaces
{
    public interface IVersionService
    {
        Task<LatestVersion> GetLatestVersion();
    }
}
