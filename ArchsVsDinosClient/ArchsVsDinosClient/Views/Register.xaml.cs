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
            SoundButton.PlayDestroyingRockSound();
            MessageBox.Show(Lang.Register_CancelledRegistration);
            this.Close();
        }

        private void Btn_RegisterNow(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            string nameTxt = TxtB_Name.Text;
            string usernameTxt = TxtB_Username.Text;
            string emailTxt = TxtB_Email.Text;
            string passwordTxt = TxtB_Password.Text;
            string nicknameTxt = TxtB_Nickname.Text;

            if (!ValidateInputs(nameTxt, usernameTxt, emailTxt, passwordTxt, nicknameTxt))
            {
                MessageBox.Show(Lang.GlobalEmptyField);
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

                RegisterResponse registered = registerClient.RegisterUser(serviceUserAccount, code);
                
                if(registered.success)
                {
                    MessageBox.Show(Lang.Register_CorrectRegister);
                    this.Close();
                }
                else
                {
                    switch(registered.resultCode)
                    {
                        case RegisterResultCode.Register_InvalidCode:
                            MessageBox.Show(Lang.Register_IncorrectCode);
                            break;
                        case RegisterResultCode.Register_BothExists:
                            MessageBox.Show(Lang.Register_UsernameAndNicknameExists);
                            break;
                        case RegisterResultCode.Register_UsernameExists:
                            MessageBox.Show(Lang.Register_UsernameAlreadyExists);
                            break;
                        case RegisterResultCode.Register_NicknameExists:
                            MessageBox.Show(Lang.Register_NicknameAlreadyExists);
                            break;
                        case RegisterResultCode.Register_UnexpectedError:
                            MessageBox.Show(Lang.GlobalServerError);
                            break;
                    }
                       
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.GlobalServerError);
            }

        }

        private bool ValidateInputs(string name, string username, string email, string password, string nickname)
        {
            if(ValidationHelper.IsEmpty(name) || ValidationHelper.IsEmpty(username) || ValidationHelper.IsEmpty(email) || ValidationHelper.IsEmpty(password) || ValidationHelper.IsEmpty(nickname))
            {
                MessageBox.Show(Lang.GlobalEmptyField);
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
