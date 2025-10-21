using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.RegisterService;
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
    /// Lógica de interacción para Register.xaml
    /// </summary>
    public partial class Register : Window
    {
        public Register()
        {
            InitializeComponent();
        }

        private void Btn_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Btn_RegisterNow(object sender, RoutedEventArgs e)
        {
            string name = txtBoxFullName.Text;
            string username = txtBoxUsername.Text;
            string email = txtBoxEmail.Text;
            string password = txtBoxPassword.Text;
            string nickname = txtBoxNickname.Text;

            if (!ValidateInputs(name, username, email, password, nickname))
            {
                return;
            }

            try
            {
                RegisterService.RegisterManagerClient registerClient = new RegisterService.RegisterManagerClient();
                //RegisterService.= registerClient.RegisterUser(UserAccountDTO, VerificationCode));

                MessageBox.Show(Lang.Register_CorrectRegister);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.Global_ServerError);
            }

        }

        private bool ValidateInputs(string name, string username, string email, string password, string nickname)
        {
            if(ValidationHelper.isEmpty(name) || ValidationHelper.isEmpty(username) || ValidationHelper.isEmpty(email) || ValidationHelper.isEmpty(password) || ValidationHelper.isEmpty(nickname))
            {
                MessageBox.Show(Lang.Global_EmptyField);
                return false;
            }

            if (!ValidationHelper.IsAValidEmail(email))
            {
                MessageBox.Show(Lang.Register_InvalidEmail);
                return false;
            }

            if (!ValidationHelper.HasPasswordAllCharacters(password) || !ValidationHelper.MinLengthPassword(password))
            {
                MessageBox.Show(Lang.Register_InvalidPassword);
                return false;
            }


            return true;
        }

    }
}
