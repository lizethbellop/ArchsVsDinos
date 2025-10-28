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
    /// Lógica de interacción para UpdateX.xaml
    /// </summary>
    public partial class UpdateX : Window
    {
        public UpdateX()
        {
            InitializeComponent();
        }

        private void Btn_Cancel(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            this.Close();
        }

        private void Btn_Save(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();

            string currentUsername = UserSession.Instance.CurrentUser.username;
            string newXLink = TxtB_XLink.Text;

            if (!ValidateInputs(newXLink))
            {
                MessageBox.Show(Lang.GlobalEmptyField);
                return;
            }

            try
            {
                ProfileManagerClient profileManagerClient = new ProfileManagerClient();
                UpdateResponse response = profileManagerClient.UpdateX(currentUsername, newXLink);

                string message = UpdateResultCodeHelper.GetMessage(response.resultCode);
                MessageBox.Show(message);

                if (response.success)
                {
                    this.Close();
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error de conexión con el servidor");
            }
        }

        private bool ValidateInputs(string xLink)
        {
            if (ValidationHelper.IsEmpty(xLink) || ValidationHelper.IsWhiteSpace(xLink))
            {
                return false;
            }

            return true;
        }
    }
}
