using ArchsVsDinosClient.Logging;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.StatisticsService;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services
{
    public class StatisticsServiceClient : IStatisticsServiceClient
    {
        private readonly StatisticsManagerClient client;
        private readonly WcfConnectionGuardian guardian;
        private bool isDisposed;

        public event Action<string, string> ConnectionError;

        public StatisticsServiceClient()
        {
            client = new StatisticsManagerClient();

            guardian = new WcfConnectionGuardian(
                onError: (title, msg) => ConnectionError?.Invoke(title, msg),
                logger: new Logger()
            );
            guardian.MonitorClientState(client);
        }

        // ═══════════════════════════════════════════════════════════════
        // MÉTODOS PÚBLICOS
        // ═══════════════════════════════════════════════════════════════

        public async Task<SaveMatchResultCode> SaveMatchStatisticsAsync(MatchResultDTO matchResult)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.SaveMatchStatistics(matchResult)),
                operationName: "guardar estadísticas"
            );
        }

        public async Task<PlayerStatisticsDTO> GetPlayerStatisticsAsync(int userId)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.GetPlayerStatistics(userId)),
                operationName: "obtener estadísticas del jugador"
            );
        }

        public async Task<LeaderboardEntryDTO[]> GetLeaderboardAsync(int topN)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.GetLeaderboard(topN)),
                operationName: "obtener leaderboard"
            );
        }

        public async Task<MatchHistoryDTO[]> GetPlayerMatchHistoryAsync(int userId, int count)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.GetPlayerMatchHistory(userId, count)),
                operationName: "obtener historial de partidas"
            );
        }

        public void Dispose()
        {
            if (isDisposed) return;

            try
            {
                if (client?.State == CommunicationState.Opened)
                    client.Close();
                else if (client?.State == CommunicationState.Faulted)
                    client.Abort();
            }
            catch
            {
                client?.Abort();
            }

            isDisposed = true;
        }
    }
}
