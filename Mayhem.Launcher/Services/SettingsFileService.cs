using CryptoMayhemLauncher.Interfaces;
using Mayhem.Dal.Dto.Dtos;
using Mayhem.Launcher.Models;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;

namespace CryptoMayhemLauncher.Services
{
    public class SettingsFileService : ISettingsFileService
    {
        private readonly string localSettingsFilePath;
        private const string errorMessageFileNotExist = "File Not exist.";
        private readonly ILogger<SettingsFileService> logger;

        public SettingsFileService(ILogger<SettingsFileService> logger)
        {
            localSettingsFilePath = Path.Combine(GetPathToProjectFolder(), "Settings.txt");
            this.logger = logger;
        }

        private string GetPathToProjectFolder()
        {
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), ".."));
        }

        public bool IsGamePathExist()
        {
            if (!IsFileExist())
            {
                logger.LogError(errorMessageFileNotExist);
                return false;
            }

            FileSettings fileSettings = GetContent();

            return !string.IsNullOrEmpty(fileSettings.GamePath.Replace("GamePath=", ""));
        }

        public bool IsFileExist()
        {
            return File.Exists(localSettingsFilePath);
        }

        public FileSettings GetContent()
        {
            FileSettings fileSettings = new FileSettings();

            foreach (string line in File.ReadLines(localSettingsFilePath))
            {
                logger.LogInformation(line);
                if (line.Contains("GamePath="))
                {
                    fileSettings.GamePath = line.Replace("GamePath=", "");
                }
                else if (line.Contains("GameVersion="))
                {
                    fileSettings.GameVersion = new BuildVersion(line.Replace("GameVersion=", ""));
                }
                else if (line.Contains("Wallet="))
                {
                    fileSettings.GameVersion = new BuildVersion(line.Replace("Wallet=", ""));
                }
            }

            return fileSettings;
        }

        public void TryCreate()
        {

            if (IsFileExist())
            {
                return;
            }

            FileSettings fileSettings = new FileSettings()
            {
                GameVersion = BuildVersion.zero,
                GamePath = string.Empty,
            };

            Save(fileSettings);
        }

        public void UpdateGameVersion(BuildVersion newBuildVersion)
        {
            FileSettings fileSettings = GetContent();
            fileSettings.GameVersion = newBuildVersion;

            Save(fileSettings);
        }

        public void UpdateWallet(string newWallet)
        {
            FileSettings fileSettings = GetContent();
            fileSettings.Wallet = newWallet;

            Save(fileSettings);
        }

        public void SetPath(string newGamePath)
        {
            FileSettings fileSettings = GetContent();
            fileSettings.GamePath = newGamePath;

            Save(fileSettings);
        }

        private void Save(FileSettings fileSettings)
        {
            string[] lines =
            {
               $"GameVersion={fileSettings.GameVersion}",
                $"GamePath={fileSettings.GamePath}",
                $"Wallet={fileSettings.Wallet}"
            };

            File.WriteAllLines(localSettingsFilePath, lines);
        }
    }
}
