using CryptoMayhemLauncher.Interfaces;
using Mayhem.Launcher.Helpers;
using Mayhem.Launcher.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Squirrel;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Mayhem.Launcher
{
    public partial class LoginWindow : Window, IActivable
    {
        public delegate void ChangeText_Handler(string text);
        public static IntPtr WindowHandle { get; private set; }

        private HttpClient httpClient;
        private readonly ILogger<LoginWindow> loggerLoginWindow;
        private static event ChangeText_Handler AuthorizationManager;
        private readonly ISettingsFileService settingsFileService;
        private readonly INavigationService navigationService;
        private readonly ILocalizationService localizationService;
        private readonly ISqurrielHandleEvents squrrielHandleEvents;

        public LoginWindow(ISqurrielHandleEvents squrrielHandleEvents, IHttpClientFactory httpClientFactory, ILogger<LoginWindow> loggerLoginWindow, ISettingsFileService settingsFileService, INavigationService navigationService, ILocalizationService localizationService)
        {
            this.settingsFileService = settingsFileService;
            this.navigationService = navigationService;
            this.localizationService = localizationService;
            this.loggerLoginWindow = loggerLoginWindow;
            this.squrrielHandleEvents = squrrielHandleEvents;
            httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Initialize();
            SetResolution(482, 600);
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


        private LoginWindowStatus status;
        public LoginWindowStatus Status
        {
            get => status;
            set
            {
                status = value;
                switch (status)
                {
                    case LoginWindowStatus.Login:
                        {
                            SetLoginContent();
                            SetActiveStackPanelVisability(nameof(AuthorizationStackPanel));
                            break;
                        }
                    case LoginWindowStatus.WaitingForBackend:
                        {
                            SetWaitingContent();
                            SetActiveStackPanelVisability(nameof(AuthorizationStackPanel));
                            break;
                        }
                    case LoginWindowStatus.Error:
                        {
                            SetActiveStackPanelVisability(nameof(ErrorStackPanel));
                            break;
                        }
                    case LoginWindowStatus.ConnectionLost:
                        {
                            SetActiveStackPanelVisability(nameof(ConnectionProblemStackPanel));
                            break;
                        }
                    case LoginWindowStatus.NotInvestor:
                        {
                            SetActiveStackPanelVisability(nameof(NotInvestorStackPanel));
                            break;
                        }
                    case LoginWindowStatus.Update:
                        {
                            SetActiveStackPanelVisability(nameof(UpdateStackPanel));
                            break;
                        }
                    default:
                        {
                            SetActiveStackPanelVisability(nameof(ErrorStackPanel));
                            loggerLoginWindow.LogError($"MSG9: Missing State: {status}");
                            break;
                        }
                }
            }
        }

        private void SetActiveStackPanelVisability(string activePanelName)
        {
            if (activePanelName == nameof(AuthorizationStackPanel))
                AuthorizationStackPanel.Visibility = Visibility.Visible;
            else
                AuthorizationStackPanel.Visibility = Visibility.Hidden;

            if (activePanelName == nameof(ErrorStackPanel))
                ErrorStackPanel.Visibility = Visibility.Visible;
            else
                ErrorStackPanel.Visibility = Visibility.Hidden;

            if (activePanelName == nameof(ConnectionProblemStackPanel))
                ConnectionProblemStackPanel.Visibility = Visibility.Visible;
            else
                ConnectionProblemStackPanel.Visibility = Visibility.Hidden;

            if (activePanelName == nameof(NotInvestorStackPanel))
            {
                LoginWindowLogo.Visibility = Visibility.Hidden;
                LoginWindowBackground.Source = new BitmapImage(ResourceAccessor.Get("Img/LoadingLageNoInvestorBackground.JPG"));
                NotInvestorStackPanel.Visibility = Visibility.Visible;
            }
            else
            {
                LoginWindowLogo.Visibility = Visibility.Visible;
                LoginWindowBackground.Source = new BitmapImage(ResourceAccessor.Get("Img/PathSettingsBackgroundBlur.png"));
                NotInvestorStackPanel.Visibility = Visibility.Hidden;
            }

            if (activePanelName == nameof(UpdateStackPanel))
                UpdateStackPanel.Visibility = Visibility.Visible;
            else
                UpdateStackPanel.Visibility = Visibility.Hidden;
        }

        private void Initialize()
        {
            AuthorizationManager += TryUpdateVersion;
            AuthorizationManager += SetTicketContentText;
            AuthorizationManager += RunProcess;
            localizationService.SetLocalization(this);
            InitializeComponent();
            RunDispatcherTimerJob();
            SetDefaultLanguageImage();
            Loaded += (s, e) =>
            {
                LoginWindowLoaded(s, e);
                LoginWindow.WindowHandle = new WindowInteropHelper(this).Handle;
                HwndSource hwndSource = HwndSource.FromHwnd(LoginWindow.WindowHandle);
                hwndSource.AddHook(new HwndSourceHook(HandleMessages));
            };
        }

        private async void TryUpdateVersion(string text)
        {
            if(text.Contains("--squirrel-updated") == false)
            {
                return;
            }

            string newVersion = getNewVersion(text);

            loggerLoginWindow.LogError($"Ticket: {newVersion}");
            loggerLoginWindow.LogError($"Ticket: {newVersion.Length}");

            if (newVersion.Length != 5)
            {
                return;
            }

            try
            {
                using (UpdateManager manager = await UpdateManager.GitHubUpdateManager(@"https://github.com/AdriaGames/CryptoMayhemLauncher"))
                {
                    squrrielHandleEvents.UpdateApp(manager, $"app-{newVersion}");
                }
            }
            catch (Exception ex)
            {
                loggerLoginWindow.LogError($"TryUpdateVersion: Internet Connection problem: {ex.Message}");
            }
        }

        private string getNewVersion(string text)
        {
            return text.Replace("--squirrel-updated,", "");
        }

        public void UpdateLocalization()
        {
            localizationService.SetLocalization(this);
            SetDefaultLanguageImage();
        }

        private void RunDispatcherTimerJob()//TODO wyłączyć jeżeli okno nie będzie aktywne?
        {
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (UnsafeNative.IsConnectedToInternet() == false)
            {
                Status = LoginWindowStatus.ConnectionLost;
            }
            else if (Status == LoginWindowStatus.ConnectionLost)
            {
                //check Launcher update TODO
                if (Status == LoginWindowStatus.ConnectionLost)
                {
                    Status = LoginWindowStatus.Login;
                }
            }
        }

        private async void RunProcess(string ticket)
        {
            if (UnsafeNative.IsConnectedToInternet() == false)
            {
                Status = LoginWindowStatus.ConnectionLost;
                return;
            }

            try
            {
                AuthorizationApiRequest authorizationApiRequest = new AuthorizationApiRequest
                {
                    Ticket = ticket,
                };

                string jsonRequest = System.Text.Json.JsonSerializer.Serialize(authorizationApiRequest);

                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync($"https://adriagames-mayhemtdsauthorization-api.azurewebsites.net/api/Authorization/Login", content);
                var apiResult = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {

                    AuthorizationSuccesApiResponse authorizationApiResponse = JsonConvert.DeserializeObject<AuthorizationSuccesApiResponse>(apiResult);

                    settingsFileService.UpdateWallet(authorizationApiResponse.Wallet);
                    settingsFileService.SetInvestorTicket(ticket);
                    navigationService.Show<MainWindow>();
                    navigationService.Hide<LoginWindow>();
                }
                else
                {
                    AuthorizationErrorApiResponse authorizationErrorApiResponse = JsonConvert.DeserializeObject<AuthorizationErrorApiResponse>(apiResult);

                    if(authorizationErrorApiResponse.Code == "ACCESS_DENIED")
                    {
                        Status = LoginWindowStatus.NotInvestor;
                        loggerLoginWindow.LogError($"Is not an investor: {ticket}");
                    }
                    else
                    {
                        Status = LoginWindowStatus.Error;
                        loggerLoginWindow.LogError($"MSG1: Error occured during Mayhem Auth Api on AuthRunProcess: {ticket}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (UnsafeNative.IsConnectedToInternet() == false)
                {
                    Status = LoginWindowStatus.ConnectionLost;
                    return;
                }

                Status = LoginWindowStatus.Error;
                loggerLoginWindow.LogError(ex, $"MSG2: Error occured during Mayhem Auth Api on AuthRunProcess: {ticket}, ExceptionMessage: {ex.Message}");
            }
        }

        private async void LoginWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (UnsafeNative.IsConnectedToInternet() == false)
            {
                Status = LoginWindowStatus.ConnectionLost;
                return;
            }

            try
            {
                using (UpdateManager manager = await UpdateManager.GitHubUpdateManager(@"https://github.com/AdriaGames/CryptoMayhemLauncher"))
                {
                    SetVersion(manager);
                }
            }
            catch (Exception ex)
            {
                loggerLoginWindow.LogError($"Current version is not uploaded on GitHub. Error Message: {ex.Message} StackTrace: {ex.StackTrace}");
            }

            try
            {
                await CheckUpdateOrStart();
            }
            catch (Exception ex)
            {
                loggerLoginWindow.LogError($"Update status. Error Message: {ex.Message} StackTrace: {ex.StackTrace}");
            }
        }

        private async Task CheckUpdateOrStart()
        {
            loggerLoginWindow.LogInformation($"CheckUpdateOrStart start");
            UpdateInfo updateInfo = null;
            using (UpdateManager manager = await UpdateManager.GitHubUpdateManager(@"https://github.com/AdriaGames/CryptoMayhemLauncher"))
            {
                updateInfo = await manager.CheckForUpdate();
            }

            if (updateInfo.ReleasesToApply.Count > 0)
            {
                loggerLoginWindow.LogInformation($"CheckUpdateOrStart Update");
                Status = LoginWindowStatus.Update;
                await UpdateApplication();
            }
            else
            {
                loggerLoginWindow.LogInformation($"CheckUpdateOrStart Login");
                Status = LoginWindowStatus.Login;
            }

            loggerLoginWindow.LogInformation($"CheckUpdateOrStart end");
        }

        private static IntPtr HandleMessages(IntPtr handle, int message, IntPtr wParameter, IntPtr lParameter, ref Boolean handled)
        {
            var data = UnsafeNative.GetMessage(message, lParameter);

            if (data != null)
            {
                var loginWindowInstance = UnsafeNative.FindWindow(null, "LoginWindow");
                if (loginWindowInstance == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                //if (Application.Current.MainWindow.WindowState == WindowState.Minimized)//TODO odblokować ten kod. Aktualnie przy pierwszym uruchomieniu Application.Current.MainWindow ma nieprawidłową wartosć.
                //Application.Current.MainWindow.WindowState = WindowState.Normal;

                UnsafeNative.SetForegroundWindow(loginWindowInstance);

                var args = data.Split(' ');
                HandleParameter(args);
                handled = true;
            }

            return IntPtr.Zero;
        }

        void SetTicketContentText(string text)
        {
            loggerLoginWindow.LogInformation($"Login with ticket: {text}");
        }

        private static void HandleParameter(string[] args)
        {
            string result = string.Join(",", args);
            result = result.Replace("mayhemlauncher:///?data=","");
            AuthorizationManager?.Invoke(result);
        }

        private void SetVersion(UpdateManager manager)
        {
            LauncherVersionText.Text = $"V{manager.CurrentlyInstalledVersion()}";
            LauncherVersionText.Visibility = Visibility.Visible;
        }

        public Task ActivateAsync(object parameter)
        {
            return Task.CompletedTask;
        }

        private void MinimalizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void GoToWebAuthorizationButton_Click(object sender, RoutedEventArgs e)
        {
            Status = LoginWindowStatus.WaitingForBackend;
            GoToLoginPage();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }



        private void SetWaitingContent()
        {
            string currentLocalization = localizationService.GetDefaultLanguage();

            if(currentLocalization == "pl")
            {
                CurrentActionText.Text = "Oczekiwanie na połączenie z portfelem ...";
                LoginButton.Content = "SPRÓBUJ PONOWNIE";
            }
            else
            {
                CurrentActionText.Text = "Waiting for connection to wallet ...";
                LoginButton.Content = "TRY AGAIN";
            }
        }

        private void SetLoginContent()
        {
            string currentLocalization = localizationService.GetDefaultLanguage();

            if (currentLocalization == "pl")
            {
                CurrentActionText.Text = "Logowanie portfelem";
                LoginButton.Content = "POŁĄCZ PORTFEL";
            }
            else
            {
                CurrentActionText.Text = "Wallet login";
                LoginButton.Content = "CONNECT WALLET";
            }
        }

        private void GoToLoginPage()
        {
            if(UnsafeNative.IsConnectedToInternet() == false)
            {
                Status = LoginWindowStatus.ConnectionLost;
                return;
            }

            string currentLocalization = localizationService.GetDefaultLanguage();
            string url = $"https:////play.cryptomayhem.io/{currentLocalization}/launcher/";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void GoToSupportPage()
        {
            if (UnsafeNative.IsConnectedToInternet() == false)
            {
                Status = LoginWindowStatus.ConnectionLost;
                return;
            }

            string url = "https://t.me/CryptoMayhemTechnicalSupport/";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void GoToProjectInformation()
        {
            if (UnsafeNative.IsConnectedToInternet() == false)
            {
                Status = LoginWindowStatus.ConnectionLost;
                return;
            }

            string url = "https://play.cryptomayhem.io/presale/";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void OpenLocalizationPopUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (SetLanguagePopUpStackPanel.Visibility == Visibility.Visible)
            {
                LanguageArrowRightRotateTransform.Angle = 90;
                SetLanguagePopUpStackPanel.Visibility = Visibility.Hidden;
            }
            else
            {
                LanguageArrowRightRotateTransform.Angle = -90;
                SetLanguagePopUpStackPanel.Visibility = Visibility.Visible;
            }
        }

        private void SetEnglishPopUp_Click(object sender, RoutedEventArgs e)
        {
            OpenLocalizationPopUpButton_Click(sender, e);
            OpenLocalizationPopUpImage.Source = new BitmapImage(ResourceAccessor.Get("Img/Button/FlagEnglishHover.png"));
            settingsFileService.SetCurrentCulture("en");
            localizationService.SetLocalization(this);
            RefreshLoginContent();
        }

        private void SetPolishPopUp_Click(object sender, RoutedEventArgs e)
        {
            OpenLocalizationPopUpButton_Click(sender, e);
            OpenLocalizationPopUpImage.Source = new BitmapImage(ResourceAccessor.Get("Img/Button/FlagPolishHover.png"));
            settingsFileService.SetCurrentCulture("pl");
            localizationService.SetLocalization(this);
            RefreshLoginContent();
        }


        private void RefreshLoginContent()
        {
            if(Status == LoginWindowStatus.Login || Status == LoginWindowStatus.WaitingForBackend)
            {
                Status = Status;
            }
        }

        private void SetDefaultLanguageImage()
        {
            if (localizationService.GetDefaultLanguage() == "pl")
            {
                OpenLocalizationPopUpImage.Source = new BitmapImage(ResourceAccessor.Get("Img/Button/FlagPolishHover.png"));
            }
            else
            {
                OpenLocalizationPopUpImage.Source = new BitmapImage(ResourceAccessor.Get("Img/Button/FlagEnglishHover.png"));
            }
        }

        private void GoToSupportButton_Click(object sender, RoutedEventArgs e)
        {
            GoToSupportPage();
        }

        private void GoToLauncherButton_Click(object sender, RoutedEventArgs e)
        {
            Status = LoginWindowStatus.Login;
        }

        private void GoToProjectButton_Click(object sender, RoutedEventArgs e)
        {
            GoToProjectInformation();
        }

        private async Task UpdateApplication()
        {

            if (UnsafeNative.IsConnectedToInternet() == false)
            {
                Status = LoginWindowStatus.ConnectionLost;
                return;
            }


            try
            {
                string executable = string.Empty;
                using (UpdateManager manager = await UpdateManager.GitHubUpdateManager(@"https://github.com/AdriaGames/CryptoMayhemLauncher"))
                {
                    loggerLoginWindow.LogError($"UpdateApplication.manager.UpdateApp");
                    await manager.UpdateApp();

                    loggerLoginWindow.LogError($"UpdateApplication.manager.CheckForUpdate");
                    var updateInfo = await manager.CheckForUpdate();

                    loggerLoginWindow.LogError($"UpdateApplication.newVersion");
                    string newVersion = string.Concat("app-",
                                            updateInfo.FutureReleaseEntry.Version.Version.Major, ".",
                                            updateInfo.FutureReleaseEntry.Version.Version.Minor, ".",
                                            updateInfo.FutureReleaseEntry.Version.Version.Build);
                    loggerLoginWindow.LogError($"UpdateApplication.executable");
                    executable = Path.Combine(manager.RootAppDirectory,
                                newVersion,
                                "Mayhem.Launcher.exe");
                }



                loggerLoginWindow.LogError($"UpdateApplication.RestartApp");
                UpdateManager.RestartApp(executable);
                loggerLoginWindow.LogError($"UpdateApplication.Exit");
                Thread.Sleep(1000);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Status = LoginWindowStatus.Error;
                loggerLoginWindow.LogError($"UpdateApplication. Error Message: {ex.Message}, StackTrace: {ex.StackTrace}");
            }

        }
    }
}
