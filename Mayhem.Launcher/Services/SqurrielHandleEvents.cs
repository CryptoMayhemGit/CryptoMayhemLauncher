using CryptoMayhemLauncher.Interfaces;
using IWshRuntimeLibrary;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Squirrel;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace CryptoMayhemLauncher.Services
{
    public class SqurrielHandleEvents : ISqurrielHandleEvents
    {
        private readonly ILogger<SqurrielHandleEvents> logger;

        public SqurrielHandleEvents(ILogger<SqurrielHandleEvents> logger)
        {
            this.logger = logger;
        }

        public void SetDefaultConfiguration()
        {
            using (UpdateManager manager = new UpdateManager(@"https://github.com/PawelSpionkowskiAdriaGames/LauncherTest"))
            {
                SquirrelAwareApp.HandleEvents(
                 onInitialInstall: v => PrepareApp(manager, v),
                 onAppUpdate: v => UpdateApp(manager, v),
                 onAppUninstall: v => RemoveApp());
            }
        }

        public void UpdateApp(UpdateManager manager, Version v)
        {
            RemoveApp();
            PrepareApp(manager, v);
        }

        private void RemoveApp()
        {
            DeleteDesktopShortcut();
            DeleteStartMenuShortcut();
            RemoveProtocol();
        }

        private void DeleteDesktopShortcut()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            System.IO.File.Delete(Path.Combine(desktopPath, "Mayhem Launcher.lnk"));
        }

        private void PrepareApp(UpdateManager manager, Version v)
        {
            string exeName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            string rootDirectory = manager.RootAppDirectory;
            RemoveStubFile(exeName, rootDirectory);

            string latestRunFilePath = GetLatestRunFilePath(v, exeName, rootDirectory);

            CreateDesktopShortcut(latestRunFilePath);
            CreateStartMenuShortcut(latestRunFilePath);
            AddProtocol(latestRunFilePath);
        }

        private static void CreateDesktopShortcut(string latestRunFilePath)
        {
            object shDesktop = "Desktop";
            WshShell shell = new WshShell();
            string shortcutAddress = $"{(string)shell.SpecialFolders.Item(ref shDesktop)}\\Mayhem Launcher.lnk";
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = "Mayhem Launcher";
            shortcut.Hotkey = "Ctrl+Shift+N";
            shortcut.TargetPath = latestRunFilePath;
            shortcut.Save();
        }

        private static string GetLatestRunFilePath(Version v, string exeName, string rootDirectory)
        {
            string updateVersionFolder = Path.Combine(rootDirectory, $"app-{v.Major}.{v.Minor}.{v.Build}");
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

        private static void CreateStartMenuShortcut(string latestRunFilePath)
        {
            string programs_path = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            string shortcutFolder = Path.Combine(programs_path, @"AdriaGames");
            if (!Directory.Exists(shortcutFolder))
            {
                Directory.CreateDirectory(shortcutFolder);
            }
            WshShell shellClass = new WshShell();
            //Create First Shortcut for Application Settings
            string settingsLink = Path.Combine(shortcutFolder, "Mayhem Launcher.lnk");
            IWshShortcut shortcut = (IWshShortcut)shellClass.CreateShortcut(settingsLink);
            shortcut.TargetPath = latestRunFilePath;
            shortcut.IconLocation = @"C:\Unity\MayhemLauncherTDS\Mayhem.Launcher\Img\Icons\Icon.ico";//ToDo direct path.
            shortcut.Arguments = "arg1 arg2";//TODO fix params
            shortcut.Description = "Click to edit MorganApp settings";//TODO fix params
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

            key.CreateSubKey(@"shell\open\command").SetValue("", latestRunFilePath + " %1");

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
