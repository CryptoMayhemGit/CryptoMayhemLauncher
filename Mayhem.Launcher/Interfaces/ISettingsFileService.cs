using Mayhem.Dal.Dto.Dtos;
using Mayhem.Launcher.Models;

namespace CryptoMayhemLauncher.Interfaces
{
    public interface ISettingsFileService
    {
        bool IsFileExist();
        bool IsGamePathExist();
        void TryCreate();
        FileSettings GetContent();
        void UpdateGameVersion(BuildVersion newBuildVersion);
        void SetPath(string newPath);
        void UpdateWallet(string newWallet);
        void SetCurrentCulture(string newCurrentCulture);
    }
}
