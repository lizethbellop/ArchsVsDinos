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
    /// Lógica de interacción para EditPassword.xaml
    /// </summary>
    public partial class EditPassword : Window
    {
        public EditPassword()
        {
            InitializeComponent();
        }

        private void Btn_Cancel(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayClick();
            this.Close();
        }

        private void Btn_Save(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayClick();

            string currentUsername = UserSession.Instance.CurrentUser.username;
            string currentPassword = Pb_CurrentPassword.Password;
            string newPassword = Pb_NewPasword.Password;

            if (!ValidateInputs(currentPassword, newPassword))
            {
                MessageBox.Show(Lang.GlobalEmptyField);
                return;
            }

            try
            {
                ProfileManagerClient profileManagerClient = new ProfileManagerClient();
                UpdateResponse response = profileManagerClient.ChangePassword(currentUsername, currentPassword, newPassword);

                if (response.success)
                {
                    MessageBox.Show("Contraseña cambiada correctamente");
                    this.Close();
                }
                else
                {

                    MessageBox.Show($"Error: {response.resultCode}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error de conexión con el servidor");
            }

        }

        private bool ValidateInputs(string currentPassword, string newPassword)
        {
            if (ValidationHelper.isEmpty(currentPassword) || ValidationHelper.isWhiteSpace(currentPassword) 
                || ValidationHelper.isEmpty(newPassword) || ValidationHelper.isWhiteSpace(newPassword))
            {
                return false;
            }

            return true;
        }
    }
}
