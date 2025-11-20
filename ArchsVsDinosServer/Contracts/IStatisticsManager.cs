using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Contracts.DTO.Result_Codes;
using Contracts.DTO.Statistics;

namespace Contracts
{
    [ServiceContract]
    public interface IStatisticsManager
    {
        [OperationContract]
        SaveMatchResultCode SaveMatchStatistics(MatchResultDTO matchResult);

        [OperationContract]
        PlayerStatisticsDTO GetPlayerStatistics(int userId);

        [OperationContract]
        List<LeaderboardEntryDTO> GetLeaderboard(int topN);

        [OperationContract]
        List<MatchHistoryDTO> GetPlayerMatchHistory(int userId, int count);

        [OperationContract]
        GameStatisticsDTO GetMatchStatistics(int matchId);
    }
}
