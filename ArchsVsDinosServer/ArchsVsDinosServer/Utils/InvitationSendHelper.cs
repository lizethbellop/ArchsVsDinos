using ArchsVsDinosServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Utils
{
    public class InvitationSendHelper : IInvitationSendHelper
    {
        private readonly IEmailNotificationSender emailNotificationSender;
        private readonly ILoggerHelper logger;

        public InvitationSendHelper(IEmailNotificationSender emailNotificationSender, ILoggerHelper logger)
        {
            this.emailNotificationSender = emailNotificationSender;
            this.logger = logger;
        }

        public async Task<bool> SendInvitation(string lobbyCode, string senderUsername, List<string> guests)
        {

            if(guests == null || guests.Count == 0)
            {
                logger.LogInfo($"No guests to send invitations for {lobbyCode}");
                return false;
            }

            try
            {
                foreach (var email in guests)
                {
                    await emailNotificationSender.SendMatchInvitation(email, senderUsername, lobbyCode);
                }
                return true;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError("Error trying to send the email: invalid configuration", ex);
                return false;
            }
            catch (TimeoutException)
            {
                logger.LogWarning($"Timeout sending invitations for {lobbyCode}");
                return false;
            }
            catch (Exception)
            {
                logger.LogInfo($"Unexpected error in sending invitations for {lobbyCode}");
                return false;
            }
        }
    }
}
