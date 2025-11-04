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

namespace ArchsVsDinosClient.Views.EditAccountViews
{
    /// <summary>
    /// Lógica de interacción para UpdateFacebook.xaml
    /// </summary>
    public partial class UpdateFacebook : Window
    {
        public UpdateFacebook()
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
            string newFacebookLink = TxtB_FacebookLink.Text;

            if (!ValidateInputs(newFacebookLink))
            {
                MessageBox.Show(Lang.GlobalEmptyField);
                return;
            }

            try
            {
                ProfileManagerClient profileManagerClient = new ProfileManagerClient();
                UpdateResponse response = profileManagerClient.UpdateFacebook(currentUsername, newFacebookLink);
                
                string message = UpdateResultCodeHelper.GetMessage(response.ResultCode);
                MessageBox.Show(message);

                if (response.Success)
                {
                    this.Close();
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error de conexión con el servidor");
            }
        }

        private bool ValidateInputs(string facebookLink)
        {
            if (ValidationHelper.IsEmpty(facebookLink) || ValidationHelper.IsWhiteSpace(facebookLink))
            {
                return false;
            }

            return true;
        }

    }
}
