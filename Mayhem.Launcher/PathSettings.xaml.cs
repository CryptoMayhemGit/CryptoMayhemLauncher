using CryptoMayhemLauncher.Interfaces;
using Mayhem.Launcher.Helpers;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Mayhem.Launcher
{
    /// <summary>
    /// Interaction logic for PathSettings.xaml
    /// </summary>
    public partial class PathSettings : Window, IActivable
    {
        private readonly INavigationService navigationService;
        private readonly ISettingsFileService settingsFileService;
        private readonly ILocalizationService localizationService;
        private string gameInstallPath;
        private const int INSTALL_PATH_LETTERS_COUNT = 17;

        public PathSettings(INavigationService navigationService, ISettingsFileService settingsFileService, ILocalizationService localizationService)
        {
            this.navigationService = navigationService;
            this.settingsFileService = settingsFileService;
            this.localizationService = localizationService;

            localizationService.SetLocalization(this);
            InitializeComponent();
            InitGamePath();
            SetDefaultLanguageImage();
        }

        private void InitGamePath()
        {
            gameInstallPath = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "Adria Games\\Crypto Mayhem");
            SetInstallPathContent(gameInstallPath);
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            settingsFileService.SetPath(gameInstallPath);
            CreateNewDirectory(gameInstallPath);
            navigationService.Show<LoginWindow>();
            navigationService.Close<PathSettings>();

        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog()
            {
                InitialDirectory = gameInstallPath,
                Title = "Select a Directory",
                Filter = "Directory|*.this.directory",
                FileName = "select"
            };

            if (dialog.ShowDialog() == true)
            {
                gameInstallPath = SetNewPath(dialog);
                SetInstallPathColor(gameInstallPath);
                SetInstallPathContent(gameInstallPath);
            }
        }

        private void SetInstallPathColor(string gameInstallPath)
        {
            SelectFolderTextBox.Foreground = new SolidColorBrush(Color.FromRgb(242, 242, 242));
        }

        private void SetInstallPathContent(string gameInstallPath)
        {
            if(gameInstallPath.Length <= INSTALL_PATH_LETTERS_COUNT)
            {
                SelectFolderTextBox.Text = gameInstallPath;
            }
            else
            {
                SelectFolderTextBox.Text = $"{gameInstallPath.Substring(0, INSTALL_PATH_LETTERS_COUNT)}...";
            }
        }

        private string SetNewPath(SaveFileDialog dialog)
        {
            return dialog.FileName
                .Replace("\\select.this.directory", "")
                .Replace(".this.directory", "");
        }

        private void CreateNewDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private void MinimalizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        public Task ActivateAsync(object parameter)
        {
            return Task.CompletedTask;
        }

        private void SetEnglishPopUp_Click(object sender, RoutedEventArgs e)
        {
            OpenLocalizationPopUpButton_Click(sender, e);
            OpenLocalizationPopUpImage.Source = new BitmapImage(ResourceAccessor.Get("Img/Button/FlagEnglishHover.png"));
            settingsFileService.SetCurrentCulture("en");
            localizationService.SetLocalization(this);
        }

        private void SetPolishPopUp_Click(object sender, RoutedEventArgs e)
        {
            OpenLocalizationPopUpButton_Click(sender, e);
            OpenLocalizationPopUpImage.Source = new BitmapImage(ResourceAccessor.Get("Img/Button/FlagPolishHover.png"));
            settingsFileService.SetCurrentCulture("pl");
            localizationService.SetLocalization(this);
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
    }
}
