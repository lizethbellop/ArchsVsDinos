using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    public enum SwapCardResultCode
    {
        SwapCard_Success,
        SwapCard_UnexpectedError,
        SwapCard_MatchNotFound,
        SwapCard_NotInTurn,
        SwapCard_NoActionsRemaining,
        SwapCard_InvalidTargetOrCard,
        SwapCard_CardPartTypeMismatch,
        SwapCard_TargetPlayerHasNoMatchingCard,
        SwapCard_InvalidCardTypeForSwap,
    }
}
