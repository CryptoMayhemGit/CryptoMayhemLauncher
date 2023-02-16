using CryptoMayhemLauncher.Interfaces;
using IWshRuntimeLibrary;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Squirrel;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace CryptoMayhemLauncher.Services
{
    public class SqurrielHandleEvents : ISqurrielHandleEvents
    {
        private readonly ISettingsFileService settingsFileService;
        private readonly ILogger<SqurrielHandleEvents> logger;

        public SqurrielHandleEvents(ISettingsFileService settingsFileService, ILogger<SqurrielHandleEvents> logger)
        {
            this.settingsFileService = settingsFileService;
            this.logger = logger;
        }

        public void SetDefaultConfiguration()
        {
            using (UpdateManager manager = new UpdateManager(@"https://github.com/AdriaGames/CryptoMayhemLauncher"))
            {
                SquirrelAwareApp.HandleEvents(
                 onInitialInstall: v => InstallApp(manager, $"app-{v.Major}.{v.Minor}.{v.Build}"),
                 onAppUpdate: v => UpdateApp(manager, $"app-{v.Major}.{v.Minor}.{v.Build}"),
                 onAppUninstall: v => RemoveApp());
            }
        }

        public void UpdateApp(UpdateManager manager, string newVersion)
        {
            try
            {
                logger.LogInformation("UpdateApp Start");
                DeleteDesktopShortcut();
                DeleteStartMenuShortcut();
                RemoveProtocol();
                InstallApp(manager, newVersion);
                logger.LogInformation("UpdateApp End");
            }
            catch (Exception ex)
            {
                logger.LogError($"UpdateApp Error. ErrorMessage: {ex.Message}, StackTrace: {ex.StackTrace}");
            }
        }

        private void RemoveApp()
        {
            try
            {
                logger.LogInformation("RemoveApp Start");
                KillAllLaunchers();
                KillAllTDS();

                DeleteDesktopShortcut();
                DeleteStartMenuShortcut();
                RemoveProtocol();
                RemoveGameFolder();
                logger.LogInformation("RemoveApp End");
            }
            catch (Exception ex)
            {
                logger.LogError($"RemoveApp Error. ErrorMessage: {ex.Message}, StackTrace: {ex.StackTrace}");
            }
        }

        private void InstallApp(UpdateManager manager, string newVersion)
        {
            try
            {
                logger.LogInformation("InstallApp Start");
                string exeName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
                string rootDirectory = manager.RootAppDirectory;
                RemoveStubFile(exeName, rootDirectory);

                string latestRunFilePath = GetLatestRunFilePath(newVersion, exeName, rootDirectory);

                CreateDesktopShortcut(latestRunFilePath);
                CreateStartMenuShortcut(latestRunFilePath);
                AddProtocol(latestRunFilePath);
                logger.LogInformation("InstallApp End");
            }
            catch (Exception ex)
            {
                logger.LogError($"InstallApp Error. ErrorMessage: {ex.Message}, StackTrace: {ex.StackTrace}");
            }
    }

        private void KillAllTDS()
        {
            foreach (Process proc in Process.GetProcesses().Where(x => x.ProcessName == "Crypto Mayhem"))
            {
                proc.Kill();
            }
        }

        private void KillAllLaunchers()
        {
            Process process = Process.GetCurrentProcess();
            string processName = process.ProcessName.Replace(".vshost", "");
            foreach (Process proc in Process.GetProcesses().Where(x => (x.ProcessName == "Mayhem.Launcher" || x.ProcessName == processName) && x.Id != process.Id))
            {
                proc.Kill();
            }
        }

        private void RemoveGameFolder()
        {
            string gamePath = settingsFileService.GetContent().GamePath;
            try
            {
                Thread.Sleep(2000);
                if (Directory.Exists(gamePath) && gamePath.Contains("Mayhem"))
                {
                    Directory.Delete(gamePath, true);
                }
            }
            catch (IOException ex)
            {
                logger.LogInformation($"RemoveGameFolder Error. ErrorMessage: {ex.Message}, StackTrace: {ex.StackTrace}");
            }
        }

        private void DeleteDesktopShortcut()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            System.IO.File.Delete(Path.Combine(desktopPath, "Mayhem Launcher.lnk"));
        }

        private static string GetLatestRunFilePath(string newVersion, string exeName, string rootDirectory)
        {
            string updateVersionFolder = Path.Combine(rootDirectory, newVersion);
            string updateStubPath = Path.Combine(updateVersionFolder, $"{exeName}.exe");
            return updateStubPath;
        }

        private static void RemoveStubFile(string exeName, string rootDirectory)
        {
            string rootStubPath = Path.Combine(rootDirectory, $"{exeName}.exe");
            if (System.IO.File.Exists(rootStubPath))
            {
                System.IO.File.Delete(rootStubPath);
            }
        }

        private static void CreateDesktopShortcut(string latestRunFilePath)
        {
            object shDesktop = "Desktop";
            WshShell shell = new WshShell();
            string shortcutAddress = $"{(string)shell.SpecialFolders.Item(ref shDesktop)}\\Mayhem Launcher.lnk";
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = "Mayhem Launcher";
            shortcut.TargetPath = latestRunFilePath;
            shortcut.Save();
        }

        private static void CreateStartMenuShortcut(string latestRunFilePath)
        {
            string programs_path = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            string shortcutFolder = Path.Combine(programs_path, @"AdriaGames");
            if (!Directory.Exists(shortcutFolder))
            {
                Directory.CreateDirectory(shortcutFolder);
            }

            WshShell shellClass = new WshShell();
            string settingsLink = Path.Combine(shortcutFolder, "Mayhem Launcher.lnk");
            IWshShortcut shortcut = (IWshShortcut)shellClass.CreateShortcut(settingsLink);
            shortcut.TargetPath = latestRunFilePath;
            shortcut.Description = "Click to edit Mayhem settings";//TODO fix params
            shortcut.Save();
        }

        [DllImport("shell32.dll")]
        private static extern bool SHGetSpecialFolderPath(IntPtr hwndOwner, [Out] StringBuilder lpszPath, int nFolder, bool fCreate);

        private const int CSIDL_COMMON_STARTMENU = 0x16;

        private static void DeleteStartMenuShortcut()
        {
            DeleteCurrentUserStartMenuShortcut();
            DeleteAllUserStartMenuShortcut();
        }

        private static void DeleteAllUserStartMenuShortcut()
        {
            StringBuilder allUserProfile = new StringBuilder(260);
            SHGetSpecialFolderPath(IntPtr.Zero, allUserProfile, CSIDL_COMMON_STARTMENU, false);
            string allUsersProgramsPath = Path.Combine(allUserProfile.ToString(), "Programs");

            string alluserShortcutFolder = Path.Combine(allUsersProgramsPath, @"AdriaGames");
            RemoveDirectory(alluserShortcutFolder);
        }

        private static void DeleteCurrentUserStartMenuShortcut()
        {
            string programsPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            string shortcutFolder = Path.Combine(programsPath, @"AdriaGames");
            RemoveDirectory(shortcutFolder);
        }

        private static void RemoveDirectory(string folder)
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }

        private static void AddProtocol(string latestRunFilePath)
        {
            RegistryKey keyTest = Registry.CurrentUser.OpenSubKey("Software", true).OpenSubKey("Classes", true);

            RegistryKey key = keyTest.CreateSubKey("MayhemLauncher");
            key.SetValue("URL Protocol", "wntPing");

            key.CreateSubKey(@"shell\open\command").SetValue("", $"\"{latestRunFilePath}\" %1");

            key.Close();
        }

        private static void RemoveProtocol()
        {
            RegistryKey currentUserKey = Registry.CurrentUser
                .OpenSubKey("Software", true)
                .OpenSubKey("Classes", true);
            currentUserKey.DeleteSubKeyTree("MayhemLauncher");
        }
    }
}
