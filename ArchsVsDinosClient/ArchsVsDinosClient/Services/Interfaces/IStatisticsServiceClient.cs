using ArchsVsDinosClient.StatisticsService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services.Interfaces
{
    public interface IStatisticsServiceClient : IDisposable
    {
        event Action<string, string> ConnectionError;

        Task<SaveMatchResultCode> SaveMatchStatisticsAsync(MatchResultDTO matchResult);
        Task<PlayerStatisticsDTO> GetPlayerStatisticsAsync(int userId);
        Task<List<LeaderboardEntryDTO>> GetLeaderboardAsync(int topN);
        Task<List<MatchHistoryDTO>> GetPlayerMatchHistoryAsync(int userId, int count);
        Task<GameStatisticsDTO> GetMatchStatisticsAsync(string matchCode);

    }
}
