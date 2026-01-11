using ArchsVsDinosClient.Properties.Langs;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using ClientSettings = ArchsVsDinosClient.Properties.Settings;

namespace ArchsVsDinosClient.Utils
{
    public static class LocalizationManager
    {
        private const string DefaultCultureName = "es-MX";

        public static void ApplyFromSettings()
        {
            string cultureName = ClientSettings.Default.languageCode;
            ApplyCulture(cultureName);
        }

        public static void ApplyCulture(string cultureName)
        {
            string effectiveCultureName = string.IsNullOrWhiteSpace(cultureName)
                ? DefaultCultureName
                : cultureName;

            CultureInfo culture = new CultureInfo(effectiveCultureName);

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            Lang.Culture = culture;

            ApplyLanguageToOpenWindows(culture);
        }

        private static void ApplyLanguageToOpenWindows(CultureInfo culture)
        {
            if (Application.Current == null)
            {
                return;
            }

            XmlLanguage language = XmlLanguage.GetLanguage(culture.IetfLanguageTag);

            foreach (Window window in Application.Current.Windows)
            {
                window.Language = language;
            }
        }
    }
}
