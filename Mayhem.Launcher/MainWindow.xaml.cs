using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
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
using Vlc.DotNet.Wpf;

namespace Mayhem.Launcher
{
    public partial class MainWindow : Window, IActivable
    {
        private readonly IVersionService versionService;
        private readonly ILivepeerService livepeerService;
        private readonly ISettingsFileService settingsFileService;
        private readonly ILocalizationService localizationService;
        private readonly INavigationService navigationService;
        private readonly ILogger<MainWindow> loggerMainWindow;

        public MainWindow(INavigationService navigationService, ISettingsFileService settingsFileService, ILivepeerService livepeerService, IVersionService versionService, ILogger<MainWindow> loggerMainWindow, ILocalizationService localizationService)
        {
            this.navigationService = navigationService;
            this.livepeerService = livepeerService;
            this.loggerMainWindow = loggerMainWindow;
            this.settingsFileService = settingsFileService;
            this.versionService = versionService;
            this.localizationService = localizationService;
            Initialize();
            SetResolution(1536, 864);
        }

        private void SetResolution(int currentWidth, int currentHeight)
        {
            var widthWorkAreaResolution = SystemParameters.WorkArea.Width;
            var percentageResolution = (widthWorkAreaResolution / 1921) * 100;
            this.MaxWidth = (percentageResolution / 100) * currentWidth;
            this.Width = (percentageResolution / 100) * currentWidth;
            this.MaxHeight = (percentageResolution / 100) * currentHeight;
            this.Height = (percentageResolution / 100) * currentHeight;
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
                    case LauncherStatus.Install:
                        {
                            break;
                        }
                    case LauncherStatus.Ready:
                        {
                            TopDownShooterNewUpdateInProgressStackPanel.Visibility = Visibility.Hidden;
                            TopDownShooterNewUpdateStackPanel.Visibility = Visibility.Hidden;
                            TopDownShooterPlayStackPanel.Visibility = Visibility.Visible;
                            break;
                        }
                    case LauncherStatus.Failed:
                        {
                            TopDownShooterNewUpdateInProgressStackPanel.Visibility = Visibility.Hidden;
                            TopDownShooterNewUpdateStackPanel.Visibility = Visibility.Visible;
                            TopDownShooterPlayStackPanel.Visibility = Visibility.Hidden;
                            break;
                        }
                    case LauncherStatus.Update:
                        {
                            TopDownShooterNewUpdateInProgressStackPanel.Visibility = Visibility.Hidden;
                            TopDownShooterNewUpdateStackPanel.Visibility = Visibility.Visible;
                            TopDownShooterPlayStackPanel.Visibility = Visibility.Hidden;
                            break;
                        }
                    case LauncherStatus.InProggres:
                        {
                            TopDownShooterNewUpdateInProgressStackPanel.Visibility = Visibility.Visible;
                            TopDownShooterNewUpdateStackPanel.Visibility = Visibility.Hidden;
                            TopDownShooterPlayStackPanel.Visibility = Visibility.Hidden;
                            break;
                        }
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

        private void RunVideo()
        {
            var currentAssembly = Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            // Default installation path of VideoLAN.LibVLC.Windows
            var libDirectory = new DirectoryInfo(Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));
            vlcPlayer.SourceProvider.CreatePlayer(libDirectory/* pass your player parameters here */);
            vlcPlayer.SourceProvider.MediaPlayer.Play(new Uri(livepeerService.GetNextAssetUrl()));
            vlcPlayer.SourceProvider.MediaPlayer.EndReached += MediaPlayer_EndReached;

            vlcPlayer.SourceProvider.MediaPlayer.Audio.IsMute = true;
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(_ => vlcPlayer.SourceProvider.MediaPlayer.Play(new Uri(livepeerService.GetNextAssetUrl())));
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
            Status = LauncherStatus.Install;
            FileSettings fileSettings = settingsFileService.GetContent();

            SetInternetConnectionStatus();

            if (fileSettings.GameVersion.IsDifferentThan(BuildVersion.zero))
            {
                SetGameIsInstalled();
                Status = LauncherStatus.Ready;
            }

            if (Status == LauncherStatus.Ready)
            {
                CheckForUpdates();
            }
        }

        private void SetPaths(UpdateManager manager)
        {
            rootPath = settingsFileService.GetContent().GamePath;
            gameZip = Path.Combine(manager.RootAppDirectory, "Build.zip");
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
            if (UnsafeNative.IsConnectedToInternet() == false)
            {
                return;
            }

            BuildVersion localVersion = GetLocalVersion();

            try
            {
                LatestVersion latestVersion = await versionService.GetLatestVersion();
                BuildVersion onlineVersion = latestVersion.Version;

                if (onlineVersion.IsDifferentThan(localVersion))
                {
                    Status = LauncherStatus.Update;
                }
                else
                {
                    Status = LauncherStatus.Ready;
                }
            }
            catch (Exception ex)
            {
                loggerMainWindow.LogError($"Error checking for game updates: {ex}");
            }
        }

        private async void InstallGame()
        {
            if (UnsafeNative.IsConnectedToInternet() == false)
            {
                return;
            }

            BuildVersion localVersion = GetLocalVersion();

            try
            {
                PBar0.Value = 0;
                ProgressCounterTextBlock.Text = "0/0MB";
                ProgressBarStackPanel.Visibility = Visibility.Visible;

                LatestVersion latestVersion = await versionService.GetLatestVersion();

                WebClient webClient = new WebClient();
                //Status = LauncherStatus.downloadingUpdate;
                if (Directory.Exists(rootPath))
                {
                    Directory.Delete(rootPath, true);
                }
                Directory.CreateDirectory(rootPath);
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(InstallGameCompletedCallback);
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(InstallProgressChanged);
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
            if (UnsafeNative.IsConnectedToInternet() == false)
            {
                return;
            }

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
                    Status = LauncherStatus.Ready;
                }
            }
            catch (Exception ex)
            {
                loggerMainWindow.LogError($"Error checking for game updates: {ex}");
            }
        }

        private BuildVersion GetLocalVersion()
        {
            BuildVersion localVersion = settingsFileService.GetContent().GameVersion;
            loggerMainWindow.LogInformation(localVersion.ToString());
            TopDownShooterGameVersionText.Text = $"V{localVersion}";
            UpdateGameVersionTextBlock.Text = $"V{localVersion}";

            return localVersion;
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            SetInternetConnectionStatus();

            if(UnsafeNative.IsConnectedToInternet() == false)
            {
                return;
            }

            if (Status == LauncherStatus.Ready)
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

                ShowLogOutPopup();
                DisableBackgroundButton.Visibility = Visibility.Visible;
                LostConnectionWindowStackPanel.Visibility = Visibility.Visible;
                SettingsWindowStackPanel.Visibility = Visibility.Hidden;
                InternetConnectionOfflineIcon.Visibility = Visibility.Hidden;
            }
        }

        private void InstallGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            PBar0.Value = 100;
            ProgressBarStackPanel.Visibility = Visibility.Hidden;
            try
            {
                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                }

                ZipFile.ExtractToDirectory(gameZip, rootPath);
                File.Delete(gameZip);

                BuildVersion buildVersion = ((BuildVersion)e.UserState);
                settingsFileService.UpdateGameVersion(buildVersion);

                TopDownShooterGameVersionText.Text = $"V{buildVersion}";
                UpdateGameVersionTextBlock.Text = $"V{buildVersion}";
                Status = LauncherStatus.Ready;
                SetGameIsInstalled();
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.Failed;
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

        private void InstallProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressCounterTextBlock.Text = $"{Math.Round(e.BytesReceived / 1024.0 / 1024.0, 2)}/{Math.Round(e.TotalBytesToReceive / 1024.0 / 1024.0, 2)} MB";
            PBar0.Value = e.ProgressPercentage;
        }

        private void UpdateProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            GameUpdateProgressBarTextBlock.Text = $"{Math.Round(e.BytesReceived / 1024.0 / 1024.0, 2)}/{Math.Round(e.TotalBytesToReceive / 1024.0 / 1024.0, 2)} MB";
            GameUpdateProgressBar.Value = e.ProgressPercentage;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await livepeerService.Init();
            RunVideo();
            SetMetodOnTop();

            try
            {
                using (UpdateManager manager = await UpdateManager.GitHubUpdateManager(@"https://github.com/AdriaGames/CryptoMayhemLauncher"))
                {
                    CurrentVersionTextBox.Text = $"V{manager.CurrentlyInstalledVersion()}";
                    SetPaths(manager);
                }
            }
            catch (Exception ex)
            {
                loggerMainWindow.LogError($"Current version is not uploaded on GitHub. Error Message: {ex.Message}");
            }

            RunDispatcherTimerJob();
        }

        private void SetMetodOnTop()
        {
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
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
            HideLogOutPopup();

            DisableBackgroundButton.Visibility = Visibility.Visible;
            SettingsWindowStackPanel.Visibility = Visibility.Visible;

        }

        private void WalletSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (LogoutButton.Visibility == Visibility.Visible)
            {
                HideLogOutPopup();
            }
            else
            {
                ShowLogOutPopup();
            }
        }

        private void ShowLogOutPopup()
        {
            WalletArrowRightRotateTransform.Angle = -90;
            LogoutButton.Visibility = Visibility.Visible;
        }

        private void HideLogOutPopup()
        {
            WalletArrowRightRotateTransform.Angle = 90;
            LogoutButton.Visibility = Visibility.Hidden;
        }

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

        private void TermsAndConditions_Click(object sender, RoutedEventArgs e)
        {
            string currentLanguage = localizationService.GetDefaultLanguage();
            GoToWebside($"https:////cryptomayhem.io/{currentLanguage}/terms-and-conditions");
        }

        private void PrivacyPolicy_Click(object sender, RoutedEventArgs e)
        {
            string currentLanguage = localizationService.GetDefaultLanguage();
            GoToWebside($"https:////cryptomayhem.io/{currentLanguage}/privacy-policy");
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
            if (File.Exists(gameExe) && Status == LauncherStatus.Ready)
            {
                string investorTicket = settingsFileService.GetContent().InvestorTicket;
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.Arguments = $"-InvestorTicket={investorTicket}";
                Process.Start(startInfo);

                Application.Current.Shutdown();
            }
            else
            {
                UpdateGame();
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            WalletSettingsButton_Click(sender, e);
            settingsFileService.UpdateWallet(string.Empty);
            navigationService.Show<LoginWindow>();
            navigationService.Hide<MainWindow>();
        }

        private async void UpdateGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (UnsafeNative.IsConnectedToInternet() == false)
            {
                return;
            }

            Status = LauncherStatus.InProggres;

            BuildVersion localVersion = GetLocalVersion();

            try
            {
                GameUpdateProgressBar.Value = 0;
                GameUpdateProgressBarTextBlock.Text = "0/0MB";
                TopDownShooterNewUpdateInProgressStackPanel.Visibility = Visibility.Visible;

                LatestVersion latestVersion = await versionService.GetLatestVersion();

                WebClient webClient = new WebClient();
                //Status = LauncherStatus.downloadingUpdate
                if (Directory.Exists(rootPath))
                {
                    Directory.Delete(rootPath, true);
                }
                Directory.CreateDirectory(rootPath);
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(UpdateGameCompletedCallback);
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(UpdateProgressChanged);
                webClient.DownloadFileAsync(new Uri(latestVersion.BuildURL), gameZip, latestVersion.Version);
                //TODO Pamiętaj, aby wywalić nie istniejące pliki.
            }
            catch (Exception ex)
            {
                TopDownShooterNewUpdateInProgressStackPanel.Visibility = Visibility.Hidden;
                TopDownShooterNewUpdateStackPanel.Visibility = Visibility.Visible;
                Status = LauncherStatus.Update;
                loggerMainWindow.LogError($"Error checking for game updates: {ex}");
            }
        }

        private void UpdateGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            GameUpdateProgressBar.Value = 100;
            TopDownShooterNewUpdateInProgressStackPanel.Visibility = Visibility.Hidden;
            try
            {
                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                }
                ZipFile.ExtractToDirectory(gameZip, rootPath);
                File.Delete(gameZip);

                BuildVersion buildVersion = ((BuildVersion)e.UserState);
                settingsFileService.UpdateGameVersion(buildVersion);

                TopDownShooterGameVersionText.Text = $"V{buildVersion}";
                UpdateGameVersionTextBlock.Text = $"V{buildVersion}";
                Status = LauncherStatus.Ready;
                SetGameIsInstalled();
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.Failed;
                loggerMainWindow.LogError($"Error finishing download: {ex}");
            }
        }
    }
}
