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
        private StatisticsManagerClient client;
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

        public async Task<SaveMatchResultCode> SaveMatchStatisticsAsync(MatchResultDTO matchResult)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.SaveMatchStatistics(matchResult)),
                defaultValue: SaveMatchResultCode.UnexpectedError,
                operationName: "save statistics"
            );
        }

        public async Task<PlayerStatisticsDTO> GetPlayerStatisticsAsync(int userId)
        {
            var currentClient = GetClient();
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => currentClient.GetPlayerStatistics(userId)),
                defaultValue: null,
                operationName: "get player statistics"
            );
        }

        public async Task<List<LeaderboardEntryDTO>> GetLeaderboardAsync(int topN)
        {
            var result = await guardian.ExecuteAsync(
                async () => await Task.Run(() =>  client.GetLeaderboard(topN)),
                defaultValue: null,
                operationName: "get leaderboard"
            );

            return result != null ? result.ToList() : new List<LeaderboardEntryDTO>();
        }

        public async Task<List<MatchHistoryDTO>> GetPlayerMatchHistoryAsync(int userId, int count)
        {
            var result = await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.GetPlayerMatchHistory(userId, count)),
                defaultValue: null,
                operationName: "get history matchs"
            );

            return result != null ? result.ToList() : new List<MatchHistoryDTO>();
        }

        public async Task<GameStatisticsDTO> GetMatchStatisticsAsync(string matchCode)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.GetMatchStatistics(matchCode)),
                defaultValue: null,
                operationName: "get match statistics"
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

        private StatisticsManagerClient GetClient()
        {
            if (client == null || client.State == CommunicationState.Faulted || client.State == CommunicationState.Closed)
            {
                if (client?.State == CommunicationState.Faulted)
                {
                    client.Abort();
                }

                var newClient = new StatisticsManagerClient();

                this.GetType().GetField("client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(this, newClient);

                guardian.MonitorClientState(newClient);
            }
            return client;
        }
    }
}

