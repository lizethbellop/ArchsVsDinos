using ArchsVsDinosClient.Logging;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.ViewModels;
using ArchsVsDinosClient.Views;
using ArchsVsDinosClient.Views.LoginRecoverPassword;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AuthenticationService = ArchsVsDinosClient.AuthenticationService;
using ClientSettings = ArchsVsDinosClient.Properties.Settings;

namespace ArchsVsDinosClient
{
    public partial class Login : Window
    {
        private const int SpanishLanguageIndex = 0;
        private const int EnglishLanguageIndex = 1;

        private const string SpanishMexicoCultureName = "es-MX";
        private const string EnglishUnitedStatesCultureName = "en-US";

        private readonly LoginViewModel viewModel;
        private readonly ILogger logger;

        private bool isLanguageComboInitialized;

        public Login()
        {
            LocalizationManager.ApplyFromSettings();

            InitializeComponent();

            viewModel = new LoginViewModel();
            DataContext = viewModel;

            viewModel.RequestClose += OnRequestClose;

            MusicPlayer.Instance.StopBackgroundMusic();

            logger = new Logger(typeof(AuthenticationServiceClient));

            InitializeLanguageCombo();
        }

        private void InitializeLanguageCombo()
        {
            isLanguageComboInitialized = false;

            string cultureName = ClientSettings.Default.languageCode;

            CB_Language.SelectedIndex = string.Equals(
                cultureName,
                EnglishUnitedStatesCultureName,
                StringComparison.OrdinalIgnoreCase)
                ? EnglishLanguageIndex
                : SpanishLanguageIndex;

            isLanguageComboInitialized = true;
        }

        private void Click_BtnRegister(object sender, RoutedEventArgs e)
        {
            new Register().ShowDialog();
        }

        private async void Click_BtnLogin(object sender, RoutedEventArgs e)
        {
            viewModel.Username = TxtB_Username.Text;
            viewModel.Password = PB_Password.Password;

            LoadingDisplayHelper.ShowLoading(LoadingOverlay);
            BtnEnter.IsEnabled = false;

            await Task.Yield();

            try
            {
                await viewModel.LoginAsync();
            }
            finally
            {
                LoadingDisplayHelper.HideLoading(LoadingOverlay);
                BtnEnter.IsEnabled = true;
            }
        }

        private void Click_BtnPlayAsGuest(object sender, RoutedEventArgs e)
        {
            UserSession.Instance.LoginAsGuest();

            var main = new MainWindow();
            main.Show();

            Close();
        }

        private void SelectionChanged_CbLanguage(object sender, SelectionChangedEventArgs e)
        {
            if (!isLanguageComboInitialized)
            {
                return;
            }

            string cultureName = CB_Language.SelectedIndex == EnglishLanguageIndex
                ? EnglishUnitedStatesCultureName
                : SpanishMexicoCultureName;

            ClientSettings.Default.languageCode = cultureName;
            ClientSettings.Default.Save();

            LocalizationManager.ApplyCulture(cultureName);
        }

        private void Click_BtnChangeLanguage(object sender, RoutedEventArgs e)
        {
            ReloadWindow();
        }

        private void ReloadWindow()
        {
            var newWindow = new Login();
            Application.Current.MainWindow = newWindow;
            newWindow.Show();
            Close();
        }

        private void OnRequestClose(object sender, EventArgs e)
        {
            var main = new MainWindow();
            main.Show();
            Close();
        }

        private void Click_BtnForgotPassword(object sender, RoutedEventArgs e)
        {
            var recover = new CheckUsername();
            recover.Show();
        }
    }
}
