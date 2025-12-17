using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Properties.Langs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Utils
{
    public static class LobbyResultCodeHelper
    {

        public static string GetMessage(MatchCreationResultCode resultCode)
        {
            switch (resultCode)
            {
                /*case MatchCreationResultCode.MatchCreation_Success:
                    return Lang.Lobby_CreatedSucces;*/
                case MatchCreationResultCode.MatchCreation_ServerBusy:
                    return Lang.GlobalServerError; 
                case MatchCreationResultCode.MatchCreation_InvalidSettings:
                    return Lang.GlobalServerError;
                case MatchCreationResultCode.MatchCreation_Timeout:
                    return Lang.GlobalServerError;
                default:
                    return Lang.GlobalServerError;
            }
        }

        public static string GetMessage(JoinMatchResultCode resultCode)
        {
            switch (resultCode)
            {
                /*case JoinMatchResultCode.JoinMatch_Success:
                    return Lang.Lobby_JoinedSuccess;*/
                case JoinMatchResultCode.JoinMatch_LobbyFull:
                    return Lang.Lobby_LobbyFull;
                case JoinMatchResultCode.JoinMatch_LobbyNotFound:
                    return Lang.Lobby_LobbyNotFound;
                default:
                    return Lang.GlobalServerError;
            }
        }

    }
}
