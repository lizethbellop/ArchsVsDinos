using ArchsVsDinosClient.Logging;
using ArchsVsDinosClient.Properties.Langs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.Utils
{
    public static class SocialMediaHelper
    {
        private readonly static ILogger logger = new Logging.Logger(typeof(SocialMediaHelper));

        public static void OpenSocialMediaLink(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            try
            {
                Process.Start(url);
            }
            catch (Win32Exception ex)
            {
                logger.LogDebug($"Win32Exception al abrir link: {ex.Message}");
                ShowErrorMessage(Lang.SocialMedia_ErrorOpeningLink);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogDebug($"InvalidOperationException al abrir link: {ex.Message}");
                ShowErrorMessage(Lang.SocialMedia_ErrorOpeningLink);
            }
            catch (Exception ex)
            {
                logger.LogDebug($"Error inesperado al abrir link: {ex.Message}");
                ShowErrorMessage(Lang.SocialMedia_ErrorOpeningLink);
            }
        }

        public static bool TryOpenSocialMediaLink(string url, SocialMediaPlatform platform)
        {
            if (!SocialMediaValidator.IsValidSocialMediaLink(url, platform))
            {
                ShowErrorMessage(SocialMediaValidator.GetValidationErrorMessage(platform));
                return false;
            }

            OpenSocialMediaLink(url);
            return true;
        }

        private static void ShowErrorMessage(string message)
        {
            MessageBox.Show(
                message,
                Lang.GlobalSystemError,
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }

    }
}
