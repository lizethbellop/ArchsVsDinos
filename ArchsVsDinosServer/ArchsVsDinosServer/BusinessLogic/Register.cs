using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using Contracts.DTO;
using System.Net;
using System.Data.Entity.Core;
using ArchsVsDinosServer.Default;
using System.Transactions;
using ArchsVsDinosServer.Utils;
using System.ServiceModel.Security.Tokens;


namespace ArchsVsDinosServer.BusinessLogic
{
    public class Register
    {

        public static List<VerificationCode> verificationCodes = new List<VerificationCode>();
        
        public bool RegisterUser(UserAccountDTO userAccountDTO, string code)
        {
            try
            {

                if (!CheckCode(userAccountDTO.email, code))
                { 
                    return false;
                }

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

        public string GenerateVerificationCode()
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

        public bool SendEmailRegister(string email)
        {
            try
            {
                string verificationCode = GenerateVerificationCode();
                
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("archvsdinos@gmail.com");
                mail.To.Add(email);
                mail.Subject = "Verification code - Arch vs Dinos";
                mail.Body = $"Your verification code is: {verificationCode}";
                mail.IsBodyHtml = false;

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential("archvsdinos@gmail.com", "gysm nupz tsei cyvn");
                smtp.EnableSsl = true;
                smtp.Send(mail);

                verificationCodes.Add(new VerificationCode
                {
                    Email = email,
                    Code = verificationCode,
                    Expiration = DateTime.Now.AddMinutes(10)
                });

                return true;

            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error sending email : {ex.Message}");
                return false;
            }
        }


        public bool CheckCode(string email, string code)
        {
            var dataCheck = verificationCodes.Find(x => x.Email == email && x.Code == code);
        
            if (dataCheck != null && dataCheck.Expiration > DateTime.Now)
            {
                verificationCodes.Remove(dataCheck);
                return true;
            }
            else
            {
                return false;
            }
        }

        public ValiUserNickResultDTO ValidateUserameAndNicknameResult(string newUsername, string newNickname)
        {
            try
            {

                using (var context = new ArchsVsDinosConnection())
                {
                   bool usernameExists = context.UserAccount.Any(u => u.username == newUsername);
                   bool nicknameExists = context.UserAccount.Any(u => u.nickname == newNickname);

                    if (usernameExists && nicknameExists)
                    {
                        return new ValiUserNickResultDTO { isValid = false, ReturnCont = ReturnContent.BothExists};
                    }

                    if (usernameExists)
                    {
                        return new ValiUserNickResultDTO { isValid = false, ReturnCont = ReturnContent.UsernameExists};
                    }
                    if (nicknameExists)
                    {
                        return new ValiUserNickResultDTO { isValid = false, ReturnCont = ReturnContent.NicknameExists};
                    }

                    return new ValiUserNickResultDTO { isValid = true, ReturnCont = ReturnContent.Success};

                }
            }
            catch (EntityException ex) 
            {
                Console.WriteLine($"Error validating username and nickname : {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"DETALLE DEL ERROR (InnerException): {ex.InnerException.Message}");
                }
                return new ValiUserNickResultDTO { isValid = false, ReturnCont = ReturnContent.DatabaseError };
            }

        }

    }

}
