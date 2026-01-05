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

        private readonly static ILogger log = new Logging.Logger(typeof(SocialMediaHelper));
        public static void OpenSocialMediaLink(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            Task.Run(() =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Win32Exception ex)
                {
                    
                    log.LogInfo($"Win32Exception to open link: {ex.Message}");
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ShowErrorMessage(Lang.SocialMedia_ErrorOpeningLink);
                    });
                }
                catch (Exception ex)
                {
                    log.LogDebug($"Error oppening link: {ex.Message}");
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ShowErrorMessage(Lang.SocialMedia_ErrorOpeningLink);
                    });
                }
            });
        }

        public static bool TryOpenSocialMediaLink(string url, SocialMediaPlatform platform)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                ShowErrorMessage(Lang.SocialMedia_NoLinkConfigured);
                return false;
            }

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
            System.Windows.MessageBox.Show(
                message,
                Lang.GlobalSystemError,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning
            );
        }
    }
}
