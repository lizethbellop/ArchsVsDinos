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
    /// Lógica de interacción para EditNickname.xaml
    /// </summary>
    public partial class EditNickname : Window
    {
        public EditNickname()
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
            string newNickname = TxtB_NewNickname.Text;

            if (!ValidateInputs(newNickname))
            {
                MessageBox.Show(Lang.GlobalEmptyField);
                return;
            }

            try
            {
                ProfileManagerClient profileManagerClient = new ProfileManagerClient();
                UpdateResponse response = profileManagerClient.UpdateNickname(currentUsername, newNickname);

                if (response.success)
                {
                    UserSession.Instance.CurrentUser.nickname = newNickname;
                    //Temporal
                    MessageBox.Show("Apodo actualizado correctamente");
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

        private bool ValidateInputs(string nickname)
        {
            if (ValidationHelper.isEmpty(nickname) || ValidationHelper.isWhiteSpace(nickname))
            {
                return false;
            }

            return true;
        }

    }
}
