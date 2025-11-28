using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient
{
    /// <summary>
    /// Lógica de interacción para App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(App));
        protected override void OnStartup(StartupEventArgs e)
        {
            var langCode = ArchsVsDinosClient.Properties.Settings.Default.languageCode;
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(langCode);
            base.OnStartup(e);

            log4net.Config.XmlConfigurator.Configure();

            log.Info("========================================");
            log.Info("=== ARCHS VS DINOS CLIENT INICIADO ===");
            log.Info("========================================");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            log.Info("=== APLICACIÓN CERRADA ===");
            base.OnExit(e);
        }
    }
}
