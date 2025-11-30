using ArchsVsDinosServer.BusinessLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Interfaces
{
    public interface IStrikeManager
    {
        bool IsUserBanned(int userId);
        bool CanSendMessage(int userId, string message);
        bool AddManualStrike(int userId, string strikeType, string reason);
        int GetRemainingStrikes(int userId);
        List<StrikeInfo> GetUserStrikeHistory(int userId);
        BanResult ProcessStrike(int userId, string message);
    }
}
