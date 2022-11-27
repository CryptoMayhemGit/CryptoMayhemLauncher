using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CryptoMayhemLauncher.Interfaces;
using Mayhem.Dal.Dto.Dtos;
using Mayhem.Dal.Tables;
using Mayhem.Launcher.Helpers;
using Microsoft.Extensions.Logging;
using Squirrel;

namespace Mayhem.Launcher
{
    public partial class MainWindow : Window, IActivable
    {
        private readonly IVersionService versionService;
        private readonly ISettingsFileService settingsFileService;
        private readonly ISqurrielHandleEvents squrrielHandleEvents;
        private readonly ILogger<MainWindow> loggerMainWindow;

        //private UpdateManager manager;

        public MainWindow(ISqurrielHandleEvents squrrielHandleEvents, ISettingsFileService settingsFileService, IVersionService versionService, ILogger<MainWindow> loggerMainWindow)
        {
            this.loggerMainWindow = loggerMainWindow;
            this.settingsFileService = settingsFileService;
            this.squrrielHandleEvents = squrrielHandleEvents;
            this.versionService = versionService;
            Initialize();
        }

        private string rootPath;
        private string gameZip;
        private string gameExe;

        private LauncherStatus status;
        internal LauncherStatus Status
        {
            get => status;
            set
            {
                //PlayButton.Visibility = Visibility.Visible;
                status = value;
                switch (status)
                {
                    case LauncherStatus.ready:
                        //PlayButton.Content = "Play";
                        break;
                    case LauncherStatus.failed:
                        //PlayButton.Content = "Update Failed - Retry";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        //PlayButton.Content = "Downloading Update";
                        break;
                    case LauncherStatus.install:
                        //PlayButton.Content = "Install Game";
                        break;
                    case LauncherStatus.updateGame:
                        //PlayButton.Content = "Update Game";
                        break;
                    default:
                        break;
                }
            }
        }

        private void Initialize()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                MainWindow_Loaded(s, e);
            };
        }

        private void SetPaths()
        {
            rootPath = settingsFileService.GetContent().GamePath;
            //gameZip = Path.Combine(manager.RootAppDirectory, "Build.zip");
            gameExe = rootPath + $"\\Crypto Mayhem.exe";
        }

        private void RunDispatcherTimerJob()
        {
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 10);
            dispatcherTimer.Start();
        }

        private async void CheckForUpdates()
        {
            BuildVersion localVersion = GetLocalVersion();

            try
            {
                LatestVersion latestVersion = await versionService.GetLatestVersion();
                BuildVersion onlineVersion = latestVersion.Version;

                if (localVersion.IsEquals(BuildVersion.zero))
                {
                    Status = LauncherStatus.install;
                }
                else if (onlineVersion.IsDifferentThan(localVersion))
                {
                    Status = LauncherStatus.updateGame;
                }
                else
                {
                    Status = LauncherStatus.ready;
                }
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                loggerMainWindow.LogError($"Error checking for game updates: {ex}");
            }
        }

        private async void InstallOrUpdate()
        {
            BuildVersion localVersion = GetLocalVersion();

            try
            {
                LatestVersion latestVersion = await versionService.GetLatestVersion();

                if (localVersion.IsEquals(BuildVersion.zero) || latestVersion.Version.IsDifferentThan(localVersion))
                {
                    InstallGameFiles(latestVersion);
                }
                else
                {
                    Status = LauncherStatus.ready;
                }
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                loggerMainWindow.LogError($"Error checking for game updates: {ex}");
            }
        }

        private BuildVersion GetLocalVersion()
        {
            BuildVersion localVersion = settingsFileService.GetContent().GameVersion;
            loggerMainWindow.LogInformation(localVersion.ToString());
            //VersionText.Text = localVersion.ToString();

            return localVersion;
        }

        private void InstallGameFiles(LatestVersion onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                Status = LauncherStatus.downloadingUpdate;
                Directory.Delete(rootPath, true);
                Directory.CreateDirectory(rootPath);
                //ProgressBarStatus.Value = 0;
                //ProgressBarStatus.Visibility = Visibility.Visible;
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);
                webClient.DownloadFileAsync(new Uri(onlineVersion.BuildURL), gameZip, onlineVersion.Version);
                //TODO Pamiętaj, aby wywalić nie istniejące pliki.
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                loggerMainWindow.LogError($"Error installing game files: {ex}");
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (Status == LauncherStatus.ready)
            {
                CheckForUpdates();
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            //ProgressBarStatus.Value = 100;
            //ProgressBarStatus.Visibility = Visibility.Hidden;
            try
            {
                ZipFile.ExtractToDirectory(gameZip, rootPath);
                File.Delete(gameZip);

                BuildVersion buildVersion = ((BuildVersion)e.UserState);
                settingsFileService.UpdateGameVersion(buildVersion);

                //VersionText.Text = buildVersion.ToString();
                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                loggerMainWindow.LogError($"Error finishing download: {ex}");
            }
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            //ProgressBarStatus.Value = e.ProgressPercentage;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                Process.Start(startInfo);

                Close();
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }
            else if (Status == LauncherStatus.install || Status == LauncherStatus.updateGame)
            {
                InstallOrUpdate();
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetMetodOnTop();
            //manager = await UpdateManager
            //    .GitHubUpdateManager(@"https://github.com/PawelSpionkowskiAdriaGames/LauncherTest");

            try
            {
                //CurrentVersionTextBox.Text = manager.CurrentlyInstalledVersion().ToString();
            }
            catch (Exception ex)
            {
                loggerMainWindow.LogError($"Current version is not uploaded on GitHub. Error Message: {ex.Message}");
            }

            SetPaths();
            RunDispatcherTimerJob();
            CheckForUpdates();
        }

        private void SetMetodOnTop()
        {
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }

        private async void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            //var updateInfo = await manager.CheckForUpdate();
            //
            //if (updateInfo.ReleasesToApply.Count > 0)
            //{
            //    //UpdateButton.IsEnabled = true;
            //}
            //else
            //{
            //    //UpdateButton.IsEnabled = false;
            //}
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            //await manager.UpdateApp();


            try
            {
                /*string executable = Path.Combine(manager.RootAppDirectory,
                                          string.Concat("app-",
                                                        CurrentVersionTextBox.Text),
                                          "Mayhem.Launcher.exe");

                var updateInfo = await manager.CheckForUpdate();

                string newVersion = string.Concat("app-",
                                        updateInfo.FutureReleaseEntry.Version.Version.Major, ".",
                                        updateInfo.FutureReleaseEntry.Version.Version.Minor, ".",
                                        updateInfo.FutureReleaseEntry.Version.Version.Build);
                squrrielHandleEvents.UpdateApp(manager, updateInfo.FutureReleaseEntry.Version.Version);
                executable = Path.Combine(manager.RootAppDirectory,
                          newVersion,
                          "Mayhem.Launcher.exe");
                loggerMainWindow.LogInformation("Update version => " + newVersion);
                UpdateManager.RestartApp(executable);
                Thread.Sleep(1000);
                Environment.Exit(0);*/
            }
            catch (Exception ex)
            {
                loggerMainWindow.LogError(ex.Message);
            }

        }

        private void MinimalizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        public Task ActivateAsync(object parameter)
        {
            return Task.CompletedTask;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsLanguage_Click(sender, e);

            DisableBackgroundButton.Visibility = Visibility.Visible;
            SettingsWindowStackPanel.Visibility = Visibility.Visible;

        }

        private void WalletSettingsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DisableBackground_Click(object sender, RoutedEventArgs e)
        {
            DisableBackgroundButton.Visibility = Visibility.Hidden;
            SettingsWindowStackPanel.Visibility = Visibility.Hidden;
        }

        private void QuitWindowSettings_Click(object sender, RoutedEventArgs e)
        {
            DisableBackgroundButton.Visibility = Visibility.Hidden;
            SettingsWindowStackPanel.Visibility = Visibility.Hidden;
        }

        private void SettingsDocuments_Click(object sender, RoutedEventArgs e)
        {
            SettingsLanguageActiveImage.ImageSource = null;
            SettingsLauncherUpdateActiveImage.ImageSource = null;
            SettingsDocumentsActiveImage.ImageSource = new BitmapImage(ResourceAccessor.Get("Img/Button/SettingsButtonActive.png"));

            SettingsDocuments.Visibility = Visibility.Visible;
            SettingsLauncherUpdate.Visibility = Visibility.Hidden;
            SettingsLanguageStackPanel.Visibility = Visibility.Hidden;
        }

        private void SettingsLauncherUpdate_Click(object sender, RoutedEventArgs e)
        {
            SettingsLanguageActiveImage.ImageSource = null;
            SettingsLauncherUpdateActiveImage.ImageSource = new BitmapImage(ResourceAccessor.Get("Img/Button/SettingsButtonActive.png"));
            SettingsDocumentsActiveImage.ImageSource = null;

            SettingsDocuments.Visibility = Visibility.Hidden;
            SettingsLauncherUpdate.Visibility = Visibility.Visible;
            SettingsLanguageStackPanel.Visibility = Visibility.Hidden;
        }

        private void SettingsLanguage_Click(object sender, RoutedEventArgs e)
        {
            SettingsLanguageActiveImage.ImageSource = new BitmapImage(ResourceAccessor.Get("Img/Button/SettingsButtonActive.png"));
            SettingsLauncherUpdateActiveImage.ImageSource = null;
            SettingsDocumentsActiveImage.ImageSource = null;

            SettingsDocuments.Visibility = Visibility.Hidden;
            SettingsLauncherUpdate.Visibility = Visibility.Hidden;
            SettingsLanguageStackPanel.Visibility = Visibility.Visible;
        }

        private void EnglishLanguage_Click(object sender, RoutedEventArgs e)
        {
            EnglishIcon.Source = new BitmapImage(ResourceAccessor.Get("Img/Button/LanguageRadioButtonClick.png"));
            PolishIcon.Source = new BitmapImage(ResourceAccessor.Get("Img/Button/LanguageRadioButtonDefault.png"));
        }

        private void PolishLanguage_Click(object sender, RoutedEventArgs e)
        {
            PolishIcon.Source = new BitmapImage(ResourceAccessor.Get("Img/Button/LanguageRadioButtonClick.png"));
            EnglishIcon.Source = new BitmapImage(ResourceAccessor.Get("Img/Button/LanguageRadioButtonDefault.png"));
        }

        private void ThirdPartyDocuments_Click(object sender, RoutedEventArgs e)
        {
            GoToWebside("https://www.tenset.io/en");
        }

        private void TermsAndConditions_Click(object sender, RoutedEventArgs e)
        {
            GoToWebside("https://adriagames.com/");
        }

        private void PrivacyPolicy_Click(object sender, RoutedEventArgs e)
        {
            GoToWebside("https://comcreo.com/");
        }

        private static void GoToWebside(string url)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
