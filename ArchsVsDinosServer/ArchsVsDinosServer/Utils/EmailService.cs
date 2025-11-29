using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    public class EmailService : IEmailService
    {
        public bool SendVerificationEmail(string email, string verificationCode)
        {
            try
            {
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

                return true;
            }
            catch (SmtpException ex)
            {
                LoggerHelper.LogError($"Smtp error at sending email", ex);
                return false;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Unexpected error at sending email", ex);
                return false;
            }
        }
    }
}
