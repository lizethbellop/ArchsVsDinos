using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace ArchsVsDinosClient.Utils
{
    internal static class UiLogoutCoordinator
    {
        private const int FlagFalse = 0;
        private const int FlagTrue = 1;

        private const string LogPrefix = "[UI LOGOUT]";

        private static int isLogoutInProgress;

        public static void LogoutToLogin(Action navigateToLogin)
        {
            if (navigateToLogin == null)
            {
                throw new ArgumentNullException(nameof(navigateToLogin));
            }

            int previous = Interlocked.Exchange(ref isLogoutInProgress, FlagTrue);
            if (previous == FlagTrue)
            {
                return;
            }

            try
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    try
                    {
                        Window mainWindow = Application.Current.MainWindow;

                        foreach (Window w in Application.Current.Windows)
                        {
                            if (w == null)
                            {
                                continue;
                            }

                            if (ReferenceEquals(w, mainWindow))
                            {
                                continue;
                            }

                            try
                            {
                                w.Close();
                            }
                            catch (InvalidOperationException ex)
                            {
                                Debug.WriteLine(string.Format("{0} Close window InvalidOperationException: {1}", LogPrefix, ex.Message));
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(string.Format("{0} Close window UnexpectedException: {1}", LogPrefix, ex.Message));
                            }
                        }

                        navigateToLogin();
                    }
                    catch (InvalidOperationException ex)
                    {
                        Debug.WriteLine(string.Format("{0} Dispatcher InvalidOperationException: {1}", LogPrefix, ex.Message));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(string.Format("{0} Dispatcher UnexpectedException: {1}", LogPrefix, ex.Message));
                    }
                });
            }
            finally
            {
                Interlocked.Exchange(ref isLogoutInProgress, FlagFalse);
            }
        }
    }
}