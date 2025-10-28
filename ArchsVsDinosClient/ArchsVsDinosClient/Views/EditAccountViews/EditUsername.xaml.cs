using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.ProfileManagerService;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ArchsVsDinosClient.Views
{
    /// <summary>
    /// Lógica de interacción para EditUsername.xaml
    /// </summary>
    public partial class EditUsername : Window
    {
        public EditUsername()
        {
            InitializeComponent();

        }

        private void Click_BtnCancel(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            this.Close();
        }

        private void Click_BtnSave(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();

            string currentUsername = UserSession.Instance.currentUser.username;
            string newUsername = TxtB_NewUsername.Text;

            if (!ValidateInputs(newUsername))
            {
                MessageBox.Show(Lang.GlobalEmptyField);
                return;
            }

            try
            {
                ProfileManagerClient profileManagerClient = new ProfileManagerClient();
                UpdateResponse response = profileManagerClient.UpdateUsername(currentUsername, newUsername);

                string message = UpdateResultCodeHelper.GetMessage(response.resultCode);
                MessageBox.Show(message);

                if (response.success)
                {
                    UserSession.Instance.currentUser.username = newUsername;
                    UserProfileObserver.Instance.NotifyProfileUpdated();
                    this.Close();
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error de conexión con el servidor");
            }

        }

        private bool ValidateInputs(string username)
        {
            if (ValidationHelper.IsEmpty(username) || ValidationHelper.IsWhiteSpace(username))
            {
                return false;
            }

            return true;
        }
    }
}
