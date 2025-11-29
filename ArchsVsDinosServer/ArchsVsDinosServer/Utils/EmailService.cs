using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Utils
{
    public static class EmailService
    {

        private static readonly Dictionary<string, string> config;

        static EmailService()
        {
            config = EnvLoader.LoadEnv("email.env");
        }

        private static string smtpServer => config["SMTP_SERVER"];
        private static int port => Convert.ToInt32(config["SMTP_PORT"]);
        private static string senderEmail => config["EMAIL_USER"];
        private static string senderPassword => config["EMAIL_PASSWORD"];

        private static SmtpClient BuildClient()
        {
            return new SmtpClient(smtpServer, port)
            {
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true
            };
        }

        private static void SendEmail(string to, string subject, string bodyHtml)
        {
            using (var client = BuildClient())
            using (var message = new MailMessage())
            {
                message.From = new MailAddress(senderEmail, "Archs Vs Dinos");
                message.To.Add(to);
                message.Subject = subject;
                message.Body = bodyHtml;
                message.IsBodyHtml = true;

                client.Send(message);
            }
        }

        public static void SendVerificationEmail(string email, string code)
        {
            string subject = "Your Verification Code - Archs Vs Dinos";

            string body = $@"
                <div style='width:100%;padding:20px;background:#f5f5f5;font-family:Arial, sans-serif;'>
                    <div style='max-width:500px;margin:auto;background:white;padding:25px;border-radius:10px;box-shadow:0 3px 8px rgba(0,0,0,0.1);'>
                        <h2 style='text-align:center;color:#333;'>Email Verification</h2>
                        
                        <p>Welcome!</p>
                        <p>Use the following verification code to continue your registration in <b>Archs Vs Dinos</b>:</p>

                        <div style='text-align:center;margin:20px 0;'>
                            <span style='display:inline-block;background:#4CAF50;color:white;padding:12px 24px;
                                    font-size:22px;border-radius:8px;letter-spacing:3px;'>
                                {code}
                            </span>
                        </div>

                        <p style='color:#666;'>This code is valid for 10 minutes. Do not share it with anyone.</p>

                        <hr style='margin:25px 0;border:none;border-top:1px solid #ddd;' />

                        <p style='text-align:center;font-size:12px;color:#999;'>
                            © 2025 Archs Vs Dinos — All rights reserved.
                        </p>
                    </div>
                </div>
            ";

            SendEmail(email, subject, body);
        }

        public static void SendLobbyInvitation(string email, string inviterUsername, string lobbyCode)
        {
            string subject = "You're Invited! - Archs Vs Dinos";

            string body = $@"
                <div style='width:100%;padding:20px;background:#f5f5f5;font-family:Arial, sans-serif;'>
                    <div style='max-width:500px;margin:auto;background:white;padding:25px;border-radius:10px;box-shadow:0 3px 8px rgba(0,0,0,0.1);'>
                        <h2 style='text-align:center;color:#333;'>Game Invitation</h2>
                        
                        <p><b>{inviterUsername}</b> invited you to a lobby in <b>Archs Vs Dinos</b>.</p>

                        <p>Use the following lobby code to join:</p>

                        <div style='text-align:center;margin:20px 0;'>
                            <span style='display:inline-block;background:#2196F3;color:white;padding:12px 24px;
                                    font-size:22px;border-radius:8px;letter-spacing:3px;'>
                                {lobbyCode}
                            </span>
                        </div>

                        <hr style='margin:25px 0;border:none;border-top:1px solid #ddd;' />

                        <p style='text-align:center;font-size:12px;color:#999;'>
                            © 2025 Archs Vs Dinos — All rights reserved.
                        </p>
                    </div>
                </div>
            ";

            SendEmail(email, subject, body);
        }
    }

}
