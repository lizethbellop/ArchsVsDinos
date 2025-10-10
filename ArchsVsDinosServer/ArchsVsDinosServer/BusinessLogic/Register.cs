using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using Contracts.DTO;
using System.Net;
using System.Data.Entity.Core;

namespace ArchsVsDinosServer.BusinessLogic
{
    internal class Register
    {

        private static List<VerificationCode> verificationCode = new List<VerificationCode>();
        /*
        public bool Register(UserAccountDTO userAccountDTO)
        {
            try
            {
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
                        email = userAccountDTO.Email,
                        password = SecurityHelper.HashPassword(userAccountDTO.Password),
                        name = userAccountDTO.Name,
                        username = userAccountDTO.Username,
                        nickname = userAccountDTO.Nickname,
                        idConfiguration = configuration.IdConfiguration,
                        idPlayer = player.IdPlayer
                    };

                    context.UserAccount.Add(userAccount);
                    context.SaveChanges();

                    scope.Complete();

                    return true;
                }
            }
            catch (EntityException ex)
            {
                LoggerHelper.LogError($"Database connection error at Register", ex);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Register: {ex.Message}");
                return false;
            }
        }

        public bool SendEmailRegister(string email)
        {
            try
            {
                string verificationCode = GenerateVerificationCode();
                
                var fromAddress = new MailAddress("archvsdinos@outlook.com", "ArchVsDinos");
                const string fromPassword = "tecnolizabraham2005*";
                const string subject = "Account Verification Arch vs Dinos";
                var toAddress = new MailAddress(email);

                string body = $"Verification code\n\nYour verification code is: {verificationCode}\n\n" +
                      "Enter this code into the game to confirm your account.\n\n" +
                      "If you did not request this, please ignore this message.";

                var smtp = new SmtpClient
                {
                    Host = "smtp.office365.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    smtp.Send(message);
                }

                verificationCode.Add(new VerificationCode
                {
                    Email = email,
                    Code = verificationCode,
                    Expiration = DateTime.Now.AddMinutes(10)
                });

                return true;

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private string GenerateVerificationCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var sb = new StringBuilder();
            int length = 6;

            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[random.Next(chars.Length)]);
            }

            return sb.ToString();
        }

        public bool CheckCode(string email, string code)
        {
            var dataCheck = verificationCode.Find(x => x.Email == email && x.Code == code);
        
            if(dataCheck != null && dataCheck.Expiration > DateTime.Now)
            {
                verificationCode.Remove(dataCheck);
                return true;
            }
            else
            {
                return false;
            }
        }*/

    }

}
