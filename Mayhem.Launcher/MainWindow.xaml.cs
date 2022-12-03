using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CryptoMayhemLauncher.Interfaces;
using Mayhem.Dal.Dto.Dtos;
using Mayhem.Dal.Tables;
using Mayhem.Launcher.Helpers;
using Mayhem.Launcher.Models;
using Microsoft.Extensions.Logging;
using Squirrel;

namespace Mayhem.Launcher
{
    public partial class MainWindow : Window, IActivable
    {
        private readonly IVersionService versionService;
        private readonly ISettingsFileService settingsFileService;
        private readonly ILocalizationService localizationService;
        private readonly ISqurrielHandleEvents squrrielHandleEvents;
        private readonly INavigationService navigationService;
        private readonly ILogger<MainWindow> loggerMainWindow;

        private UpdateManager manager;

        public MainWindow(INavigationService navigationService, ISqurrielHandleEvents squrrielHandleEvents, ISettingsFileService settingsFileService, IVersionService versionService, ILogger<MainWindow> loggerMainWindow, ILocalizationService localizationService)
        {
            this.navigationService = navigationService;
            this.loggerMainWindow = loggerMainWindow;
            this.settingsFileService = settingsFileService;
            this.squrrielHandleEvents = squrrielHandleEvents;
            this.versionService = versionService;
            this.localizationService = localizationService;
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
                        PlayButtonTextBlock.Text = "Graj";
                        break;
                    case LauncherStatus.failed:
                        //PlayButtonTextBlock.Text = "Play"; // TODO Co z tym?
                        break;
                    case LauncherStatus.install:
                        break;
                    case LauncherStatus.updateGame:
                        PlayButtonTextBlock.Text = "Aktualizuj";
                        break;
                    default:
                        break;
                }
            }
        }

        private void Initialize()
        {
            localizationService.SetLocalization(this);

            InitializeComponent();
            SetWalletText();
            SetEarlyStage();
            SetLanguageImage();



            Loaded += (s, e) =>
            {
                MainWindow_Loaded(s, e);
            };
        }

        public void UpdateLocalization()
        {
            localizationService.SetLocalization(this);
            SetLanguageImage();
        }

        private void SetWalletText()
        {
            string wallet = settingsFileService.GetContent().Wallet;
            LoggedWalletTextBlock.Text = $"{wallet.Substring(0, 4)}...{wallet.Substring(wallet.Length - 4)}";
        }

        private void SetEarlyStage()
        {
            Status = LauncherStatus.install;
            FileSettings fileSettings = settingsFileService.GetContent();

            SetInternetConnectionStatus();

            if (fileSettings.GameVersion.IsDifferentThan(BuildVersion.zero))
            {
                SetGameIsInstalled();
                Status = LauncherStatus.ready;
            }

            if (Status == LauncherStatus.ready)
            {
                CheckForUpdates();
            }
        }

        private void SetPaths()
        {
            rootPath = settingsFileService.GetContent().GamePath;
            //gameZip = Path.Combine(manager.RootAppDirectory, "Build.zip");
            gameExe = rootPath + $"\\Crypto Mayhem.exe";
        }

        private void RunDispatcherTimerJob()//TODO wyłączyć jeżeli okno nie będzie aktywne?
        {
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Start();
        }

        private async void CheckForUpdates()
        {
            BuildVersion localVersion = GetLocalVersion();

            try
            {
                LatestVersion latestVersion = await versionService.GetLatestVersion();
                BuildVersion onlineVersion = latestVersion.Version;

                if (onlineVersion.IsDifferentThan(localVersion))
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

        private async void InstallGame()
        {
            BuildVersion localVersion = GetLocalVersion();

            try
            {
                PBar0.Value = 0;
                ProgressCounterTextBlock.Text = "0/0MB";
                ProgressBarStackPanel.Visibility = Visibility.Visible;

                LatestVersion latestVersion = await versionService.GetLatestVersion();

                WebClient webClient = new WebClient();
                //Status = LauncherStatus.downloadingUpdate;
                Directory.Delete(rootPath, true);
                Directory.CreateDirectory(rootPath);
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);
                webClient.DownloadFileAsync(new Uri(latestVersion.BuildURL), gameZip, latestVersion.Version);
                //TODO Pamiętaj, aby wywalić nie istniejące pliki.
            }
            catch (Exception ex)
            {
                ProgressBarStackPanel.Visibility = Visibility.Hidden;
                InstallButton.Visibility = Visibility.Visible;
                //Status = LauncherStatus.failed;
                loggerMainWindow.LogError($"Error checking for game updates: {ex}");
            }
        }

        private async void UpdateGame()
        {
            BuildVersion localVersion = GetLocalVersion();

            try
            {
                LatestVersion latestVersion = await versionService.GetLatestVersion();

                if (localVersion.IsEquals(BuildVersion.zero) || latestVersion.Version.IsDifferentThan(localVersion))
                {
                    InstallGame();
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
            TopDownShooterGameVersionText.Text = $"V{localVersion}";

            return localVersion;
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            SetInternetConnectionStatus();

            if(UnsafeNative.IsConnectedToInternet() == false)
            {
                return;
            }

            if (Status == LauncherStatus.ready)
            {
                CheckForUpdates();
            }
        }

        private void SetInternetConnectionStatus()
        {
            if (UnsafeNative.IsConnectedToInternet())
            {
                InternetConnectionOfflineIcon.Visibility = Visibility.Hidden;

                if (LostConnectionWindowStackPanel.Visibility == Visibility.Visible)
                {
                    DisableBackgroundButton.Visibility = Visibility.Hidden;
                    LostConnectionWindowStackPanel.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                if (InternetConnectionOfflineIcon.Visibility == Visibility.Visible)
                    return;

                DisableBackgroundButton.Visibility = Visibility.Visible;
                LostConnectionWindowStackPanel.Visibility = Visibility.Visible;
                SettingsWindowStackPanel.Visibility = Visibility.Hidden;
                InternetConnectionOfflineIcon.Visibility = Visibility.Hidden;
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            PBar0.Value = 100;
            ProgressBarStackPanel.Visibility = Visibility.Hidden;
            try
            {
                ZipFile.ExtractToDirectory(gameZip, rootPath);
                File.Delete(gameZip);

                BuildVersion buildVersion = ((BuildVersion)e.UserState);
                settingsFileService.UpdateGameVersion(buildVersion);

                TopDownShooterGameVersionText.Text = $"V{buildVersion}";
                //Status = LauncherStatus.ready;
                SetGameIsInstalled();
            }
            catch (Exception ex)
            {
                //Status = LauncherStatus.failed;
                loggerMainWindow.LogError($"Error finishing download: {ex}");
            }
        }

        private void SetGameIsInstalled()
        {
            InstallTDSStackPanel.Visibility = Visibility.Hidden;
            TopDownShooterPlayStackPanel.Visibility = Visibility.Visible;
            InstalledGameListTitleText.Visibility = Visibility.Visible;

            NeewsStackPanel.Margin = new Thickness(443, 142, 34, 407);
            NewsTitleText.Margin = new Thickness(460,92,847,735);

            LastGameToInstall.Margin = new Thickness(22, 533, 1144, 30);
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressCounterTextBlock.Text = $"{Math.Round(e.BytesReceived / 1024.0 / 1024.0, 2)}/{Math.Round(e.TotalBytesToReceive / 1024.0 / 1024.0, 2)} MB";
            PBar0.Value = e.ProgressPercentage;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetMetodOnTop();
            //manager = await UpdateManager
            //    .GitHubUpdateManager(@"https://github.com/PawelSpionkowskiAdriaGames/LauncherTest");

            try
            {
                //CurrentVersionTextBox.Text = $"V{manager.CurrentlyInstalledVersion()}";
            }
            catch (Exception ex)
            {
                loggerMainWindow.LogError($"Current version is not uploaded on GitHub. Error Message: {ex.Message}");
            }

            SetPaths();
            RunDispatcherTimerJob();
            //CheckForUpdates();
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

        private void MinimalizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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
            if (LogoutButton.Visibility == Visibility.Visible)
            {
                WalletArrowRightRotateTransform.Angle = 90;
                LogoutButton.Visibility = Visibility.Hidden;
            }
            else
            {
                WalletArrowRightRotateTransform.Angle = -90;
                LogoutButton.Visibility = Visibility.Visible;
            }
        }

        /*
            await manager.UpdateApp();


            try
            {
                string executable = Path.Combine(manager.RootAppDirectory,
                                          string.Concat("app-",
                                                        CurrentVersionTextBox.Text.Replace("V","")),
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
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                loggerMainWindow.LogError(ex.Message);
            }
         */

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            InstallGame();
            InstallButton.Visibility = Visibility.Hidden;
        }

        private void DisableBackground_Click(object sender, RoutedEventArgs e)
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
            settingsFileService.SetCurrentCulture("en");
            SetLanguageImage();
            localizationService.SetLocalization(this);
        }

        private void PolishLanguage_Click(object sender, RoutedEventArgs e)
        {
            settingsFileService.SetCurrentCulture("pl");
            SetLanguageImage();
            localizationService.SetLocalization(this);
        }

        private void SetLanguageImage()
        {
            if(localizationService.GetDefaultLanguage() == "pl")
            {
                PolishIcon.Source = new BitmapImage(ResourceAccessor.Get("Img/Button/LanguageRadioButtonClick.png"));
                EnglishIcon.Source = new BitmapImage(ResourceAccessor.Get("Img/Button/LanguageRadioButtonDefault.png"));
            }
            else
            {
                EnglishIcon.Source = new BitmapImage(ResourceAccessor.Get("Img/Button/LanguageRadioButtonClick.png"));
                PolishIcon.Source = new BitmapImage(ResourceAccessor.Get("Img/Button/LanguageRadioButtonDefault.png"));
            }
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

        private void AgreeLostConnection_Click(object sender, RoutedEventArgs e)
        {
            QuitLostConnectionWindow_Click(sender, e);
            InternetConnectionOfflineIcon.Visibility = Visibility.Visible;

        }

        private void QuitWindowSettings_Click(object sender, RoutedEventArgs e)
        {
            DisableBackgroundButton.Visibility = Visibility.Hidden;
            SettingsWindowStackPanel.Visibility = Visibility.Hidden;
            LostConnectionWindowStackPanel.Visibility = Visibility.Hidden;
        }

        private void QuitLostConnectionWindow_Click(object sender, RoutedEventArgs e)
        {
            DisableBackgroundButton.Visibility = Visibility.Hidden;
            SettingsWindowStackPanel.Visibility = Visibility.Hidden;
            LostConnectionWindowStackPanel.Visibility = Visibility.Hidden;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                Process.Start(startInfo);

                Application.Current.Shutdown();
            }
            else
            {
                UpdateGame();
            }

            /*if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                Process.Start(startInfo);
            
            navigationService.Hide<MainWindow>();
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }
            else if (Status == LauncherStatus.install || Status == LauncherStatus.updateGame)
            {
                InstallOrUpdate();
            }*/
            /*
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }*/
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            WalletSettingsButton_Click(sender, e);
            settingsFileService.UpdateWallet(string.Empty);
            navigationService.Show<LoginWindow>();
            navigationService.Hide<MainWindow>();
        }
    }
}
