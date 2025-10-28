using ArchsVsDinosServer.Default;
using ArchsVsDinosServer.Utils;
using Contracts.DTO;
using Contracts.DTO.Response;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.ServiceModel.Security.Tokens;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Web.Management;


namespace ArchsVsDinosServer.BusinessLogic
{
    public class Register
    {

        public static List<VerificationCode> verificationCodes = new List<VerificationCode>();

        public RegisterResponse RegisterUser(UserAccountDTO userAccountDTO, string code)
        {

            try
            {
                RegisterResponse response = new RegisterResponse();

                if (CheckCode(userAccountDTO.email, code))
                {

                    var validationUsernameAndNickname = ValidateUsernameAndNicknameResult(userAccountDTO.username, userAccountDTO.nickname);
                    if (!validationUsernameAndNickname.success)
                        return validationUsernameAndNickname;

                    using (var scope = new TransactionScope())
                    using (var context = new ArchsVsDinosConnection())
                    {

                        var player = InitialConfig.InitialPlayer;
                        context.Player.Add(player);
                        context.SaveChanges();

                        var configuration = InitialConfig.InitialConfiguration;
                        context.Configuration.Add(configuration);
                        context.SaveChanges();

                        var userAccount = new UserAccount
                        {
                            email = userAccountDTO.email,
                            password = SecurityHelper.HashPassword(userAccountDTO.password),
                            name = userAccountDTO.name,
                            username = userAccountDTO.username,
                            nickname = userAccountDTO.nickname,
                            idConfiguration = configuration.idConfiguration,
                            idPlayer = player.idPlayer
                        };

                        context.UserAccount.Add(userAccount);
                        context.SaveChanges();
                        scope.Complete();

                        return new RegisterResponse
                        {
                            success = true,
                            resultCode = RegisterResultCode.Register_Success
                        };
                    }
                }
                else
                {
                    return new RegisterResponse
                    {
                        success = false,
                        resultCode = RegisterResultCode.Register_InvalidCode
                    };
                }

            }
            catch (EntityException ex)
            {
                LoggerHelper.LogError($"Database connection error at Register", ex);
                return new RegisterResponse
                {
                    success = false,
                    resultCode = RegisterResultCode.Register_DatabaseError
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Register: {ex.Message}");
                return new RegisterResponse
                {
                    success = false,
                    resultCode = RegisterResultCode.Register_UnexpectedError
                };
            }
        }

        public bool SendEmailRegister(string email)
        {
            try
            {
                string verificationCode = CodeGenerator.GenerateVerificationCode();
                
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("archvsdinos@gmail.com");
                mail.To.Add(email);
                mail.Subject = "Verification code - Arch vs Dinos";
                mail.Body = $"Hi, your verification code is: {verificationCode} \n Don't share this code with another person.";
                mail.IsBodyHtml = false;

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential("archvsdinos@gmail.com", "gysm nupz tsei cyvn");
                smtp.EnableSsl = true;
                smtp.Send(mail);

                verificationCodes.Add(new VerificationCode
                {
                    email = email,
                    code = verificationCode,
                    expiration = DateTime.Now.AddMinutes(10)
                });

                return true;

            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"Error sending email : {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email : {ex.Message}");
                return false;
            }
        }


        public bool CheckCode(string email, string code)
        {
            var dataCheck = verificationCodes.Find(x => x.email == email && x.code == code);
        
            if (dataCheck != null && dataCheck.expiration > DateTime.Now)
            {
                verificationCodes.Remove(dataCheck);
                return true;
            }
            else
            {
                return false;
            }
        }

        public RegisterResponse ValidateUsernameAndNicknameResult(string newUsername, string newNickname)
        {
            try
            {
                RegisterResponse response = new RegisterResponse();

                using (var context = new ArchsVsDinosConnection())
                {
                    bool usernameExists = context.UserAccount.Any(u => u.username == newUsername);
                    bool nicknameExists = context.UserAccount.Any(u => u.nickname == newNickname);

                    if (usernameExists && nicknameExists)
                    {
                        response.success = false;
                        response.resultCode = RegisterResultCode.Register_BothExists;
                        return response;
                    }

                    if (usernameExists)
                    {
                        response.success = false;
                        response.resultCode = RegisterResultCode.Register_UsernameExists;
                        return response;
                    }
                    if (nicknameExists)
                    {
                        response.success = false;
                        response.resultCode = RegisterResultCode.Register_NicknameExists;
                        return response;
                    }

                    response.success = true;
                    return response;

                }
            }
            catch (EntityException ex)
            {
                Console.WriteLine($"Error validating username and nickname : {ex.Message}");
                return new RegisterResponse
                {
                    success = false,
                    resultCode = RegisterResultCode.Register_DatabaseError
                };
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Error in Register: {ex.Message}");
                return new RegisterResponse
                {
                    success = false,
                    resultCode = RegisterResultCode.Register_UnexpectedError
                };
            }

        }

    }

}
