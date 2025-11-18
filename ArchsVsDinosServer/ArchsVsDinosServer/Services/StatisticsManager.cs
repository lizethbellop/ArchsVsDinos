using Contracts;
using Contracts.DTO.Result_Codes;
using Contracts.DTO.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class StatisticsManager : IStatisticsManager
    {
        // TODO: Implementar mañana

        public SaveMatchResultCode SaveMatchStatistics(MatchResultDTO matchResult)
        {
            throw new NotImplementedException();
        }

        public PlayerStatisticsDTO GetPlayerStatistics(int userId)
        {
            throw new NotImplementedException();
        }

        public List<LeaderboardEntryDTO> GetLeaderboard(int topN)
        {
            throw new NotImplementedException();
        }

        public List<MatchHistoryDTO> GetPlayerMatchHistory(int userId, int count)
        {
            throw new NotImplementedException();
        }
    }
}
