using ArchsVsDinosClient.Utils;
using log4net;
using log4net.Config;
using System.Globalization;
using System.Windows;
using ClientSettings = ArchsVsDinosClient.Properties.Settings;


namespace ArchsVsDinosClient
{
    /// <summary>
    /// Lógica de interacción para App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string DefaultCultureName = "es-MX";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(App));

        protected override void OnStartup(StartupEventArgs e)
        {
            XmlConfigurator.Configure();

            ApplyCultureFromSettings();

            base.OnStartup(e);

            Logger.Info("========================================");
            Logger.Info("=== ARCHS VS DINOS CLIENT INICIADO ===");
            Logger.Info("========================================");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("=== APLICACIÓN CERRADA ===");
            base.OnExit(e);
        }

        private static void ApplyCultureFromSettings()
        {
            string cultureName = ClientSettings.Default.languageCode;


            try
            {
                LocalizationManager.ApplyCulture(cultureName);
            }
            catch (CultureNotFoundException)
            {
                LocalizationManager.ApplyCulture(DefaultCultureName);
            }
        }
    }
}
