using CryptoMayhemLauncher.Interfaces;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Mayhem.Launcher
{
    /// <summary>
    /// Interaction logic for PathSettings.xaml
    /// </summary>
    public partial class PathSettings : Window, IActivable
    {
        private readonly INavigationService navigationService;
        private readonly ISettingsFileService settingsFileService;
        private string gameInstallPath;
        private const int INSTALL_PATH_LETTERS_COUNT = 14;

        public PathSettings(INavigationService navigationService, ISettingsFileService settingsFileService)
        {
            this.navigationService = navigationService;
            this.settingsFileService = settingsFileService;

            InitializeComponent();
            InitGamePath();
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
            Close();
            
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
    }
}
