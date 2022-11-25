using CryptoMayhemLauncher.Interfaces;
using Mayhem.Launcher.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Squirrel;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

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

        public LoginWindow(IHttpClientFactory httpClientFactory, ILogger<LoginWindow> loggerLoginWindow, ISettingsFileService settingsFileService, INavigationService navigationService)
        {
            this.loggerLoginWindow = loggerLoginWindow;
            httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Initialize();
            this.settingsFileService = settingsFileService;
            this.navigationService = navigationService;
        }

        private void Initialize()
        {
            AuthorizationManager += SetTicketContentText;
            AuthorizationManager += RunProcess;
            InitializeComponent();
            Loaded += (s, e) =>
            {
                LoginWindowLoaded(s, e);
                LoginWindow.WindowHandle = new WindowInteropHelper(this).Handle;
                HwndSource hwndSource = HwndSource.FromHwnd(LoginWindow.WindowHandle);
                hwndSource.AddHook(new HwndSourceHook(HandleMessages));
            };
        }

        private async void RunProcess(string ticket)
        {
            try
            {
                //this.WindowState = WindowState.Normal;
                AuthorizationApiRequest authorizationApiRequest = new AuthorizationApiRequest
                {
                    Ticket = ticket,
                };

                string jsonRequest = System.Text.Json.JsonSerializer.Serialize(authorizationApiRequest);

                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync($"https://mayhemtdsauthorizationapi.azurewebsites.net/api/Authorization/Login", content);
                if (response.IsSuccessStatusCode)
                {
                    var apiResult = await response.Content.ReadAsStringAsync();

                    AuthorizationApiResponse authorizationApiResponse = JsonConvert.DeserializeObject<AuthorizationApiResponse>(apiResult);

                    settingsFileService.UpdateWallet(authorizationApiResponse.Wallet);
                    navigationService.Show<MainWindow>();
                    Close();
                }
                else
                {
                    loggerLoginWindow.LogError($"MSG1: Error occured during Mayhem Auth Api on AuthRunProcess: {ticket}");
                }
            }
            catch (Exception ex)
            {
                loggerLoginWindow.LogError(ex, $"MSG2: Error occured during Mayhem Auth Api on AuthRunProcess: {ticket}, ExceptionMessage: {ex.Message}");
            }
        }

        private async void LoginWindowLoaded(object sender, RoutedEventArgs e)
        {

            //UpdateManager manager = await UpdateManager.GitHubUpdateManager(@"https://github.com/PawelSpionkowskiAdriaGames/LauncherTest");

            try
            {
                //SetVersion(manager);
            }
            catch (Exception ex)
            {
                loggerLoginWindow.LogError($"Current version is not uploaded on GitHub. Error Message: {ex.Message}");
            }
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
            Close();
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            SetWaitingContent();
            GoToLoginPage();
        }

        private void SetWaitingContent()
        {
            CurrentActionText.Text = "Oczekiwanie na połączenie z portfelem ...";
            LoginButton.Content = "SPRÓBUJ PONOWNIE";
        }

        private static void GoToLoginPage()
        {
            string url = "https://play.cryptomayhem.io/launcher/";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
