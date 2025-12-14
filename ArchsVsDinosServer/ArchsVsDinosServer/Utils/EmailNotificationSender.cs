using ArchsVsDinosServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Utils
{
    public class EmailNotificationSender : IEmailNotificationSender
    {
        public Task SendMatchInvitation(string email, string inviterUsername, string matchCode)
        {
            EmailService.SendLobbyInvitation(email, matchCode, inviterUsername);
            return Task.CompletedTask;
        }
    }
}
