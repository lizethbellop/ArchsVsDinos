using ArchsVsDinosServer.Services;
using ArchsVsDinosServer.Utils;
using Contracts.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.Moderation
{
    public class ModerationEngine
    {
        private readonly StrikeManager strikeManager;

        public ModerationEngine(ProfanityFilter profanityFilter)
        {
            var deps = new StrikeServiceDependencies
            {
                ProfanityFilter = profanityFilter
            };
            strikeManager = new StrikeManager(deps);
        }

        public ModerationResult Moderate(ModerationRequestDTO request)
        {
            var strikeResult = strikeManager.ProcessStrike(
                request.UserId,
                request.Message
            );

            string reason = "";
            if (!strikeResult.CanSendMessage)
            {
                if (strikeResult.IsGuest)
                {
                    reason = "Message blocked due to inappropriate content";
                }
                else if (strikeResult.ShouldBan)
                {
                    reason = "User expelled due to repeated inappropriate messages";
                }
                else
                {
                    reason = $"Warning {strikeResult.CurrentStrikes}/3";
                }
            }

            return new ModerationResult
            {
                CanSendMessage = strikeResult.CanSendMessage,
                ShouldBan = strikeResult.ShouldBan,
                CurrentStrikes = strikeResult.CurrentStrikes,
                IsGuest = strikeResult.IsGuest,
                Reason = reason
            };
        }
    }

}

