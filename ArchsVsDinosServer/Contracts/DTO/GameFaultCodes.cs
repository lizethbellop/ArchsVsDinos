using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    public static class GameFaultCodes
    {
        public const string InvalidParameter = "InvalidParameter";
        public const string NotYourTurn = "NotYourTurn";
        public const string NoMovesLeft = "NoMovesLeft";
        public const string InvalidCard = "InvalidCard";
        public const string GameNotActive = "GameNotActive";
        public const string PlayerNotFound = "PlayerNotFound";
        public const string InternalError = "InternalError";
    }

}
