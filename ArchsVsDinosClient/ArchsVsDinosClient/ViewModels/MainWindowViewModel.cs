using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ArchsVsDinosClient.ViewModels
{
    public class MainWindowViewModel
    {
        public async Task LogoutAsync()
        {

            if (UserSession.Instance.IsGuest || UserSession.Instance.CurrentUser == null)
            {
                UserSession.Instance.Logout();
                return;
            }

            string username = UserSession.Instance.GetUsername();

            try
            {
                using (var client = new AuthenticationServiceClient())
                {
                    await client.LogoutAsync(username);
                }
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"[LOGOUT WARNING] Timeout: {ex.Message}");
                MessageBox.Show(Lang.WcfErrorOperationTimeout);
            }
            catch (EndpointNotFoundException ex)
            {
                Debug.WriteLine($"[LOGOUT WARNING] Server not found: {ex.Message}");
                MessageBox.Show(Lang.GlobalServerNotFound);
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine($"[LOGOUT WARNING] Error de comunicación: {ex.Message}");
                MessageBox.Show(Lang.WcfErrorService);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOGOUT ERROR] Error inesperado: {ex.Message}");
                MessageBox.Show(Lang.GlobalUnexpectedError);
            }
            finally
            {
                UserSession.Instance.Logout();
            }
        }
    }
}
