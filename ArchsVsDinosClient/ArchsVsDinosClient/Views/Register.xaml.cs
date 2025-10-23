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
using UserAccountDTO = ArchsVsDinosClient.DTO.UserAccountDTO;

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
            SoundButton.PlayClick();
            this.Close();
        }

        private void Btn_RegisterNow(object sender, RoutedEventArgs e)
        {
            SoundMovingRock.PlayClick();
            string nameTxt = txtBoxFullName.Text;
            string usernameTxt = txtBoxUsername.Text;
            string emailTxt = txtBoxEmail.Text;
            string passwordTxt = txtBoxPassword.Text;
            string nicknameTxt = txtBoxNickname.Text;

            if (!ValidateInputs(nameTxt, usernameTxt, emailTxt, passwordTxt, nicknameTxt))
            {
                MessageBox.Show(Lang.Global_EmptyField);
                return;
            }

            try
            {
                RegisterManagerClient registerClient = new RegisterManagerClient();

                bool sent = registerClient.SendEmailRegister(emailTxt);
                if(!sent)
                {
                    MessageBox.Show(Lang.Register_SentErrorCode);
                    return;
                }

                ConfirmCode codeWindow = new ConfirmCode
                {
                    Owner = this
                };
                codeWindow.ShowDialog();

                if (codeWindow.IsCancelled)
                {
                    MessageBox.Show(Lang.Register_CancelledRegistration);
                    return;
                }

                string code = codeWindow.EnteredCode;

                UserAccountDTO UserAccountDTO = new UserAccountDTO
                {
                    name = nameTxt,
                    username = usernameTxt,
                    email = emailTxt,
                    password = passwordTxt,
                    nickname = nicknameTxt
                };

                var serviceUserAccount = new RegisterService.UserAccountDTO
                {
                    name = UserAccountDTO.name,
                    username = UserAccountDTO.username,
                    email = UserAccountDTO.email,
                    password = UserAccountDTO.password,
                    nickname = UserAccountDTO.nickname
                };

                var validationUsernameNickname = registerClient.ValidateUsernameAndNickname(usernameTxt, nicknameTxt);

                if(!validationUsernameNickname.isValid)
                {
                    switch (validationUsernameNickname.ReturnCont)
                    {
                        case ReturnContent.BothExists:
                            MessageBox.Show(Lang.Register_UsernameAndNicknameExists);
                            break;
                        case ReturnContent.UsernameExists:
                            MessageBox.Show(Lang.Register_UsernameAlreadyExists);
                            break;
                        case ReturnContent.NicknameExists:
                            MessageBox.Show(Lang.Register_NicknameAlreadyExists);
                            break;
                        case ReturnContent.DatabaseError:
                            MessageBox.Show(Lang.Global_ServerError);
                            break;
                    }
                    return;
                }

                bool registered = registerClient.RegisterUser(serviceUserAccount, code);
                
                if(registered)
                {
                    MessageBox.Show(Lang.Register_CorrectRegister);
                    this.Close();
                }
                else
                {
                    MessageBox.Show(Lang.Register_IncorrectCode);
                }

              
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
