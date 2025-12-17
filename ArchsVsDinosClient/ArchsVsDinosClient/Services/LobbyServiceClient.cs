using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Logging;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services
{
    public class LobbyServiceClient : ILobbyServiceClient
    {
        private readonly LobbyManagerClient lobbyManagerClient;
        private readonly LobbyCallbackManager lobbyCallbackManager;
        private readonly WcfConnectionGuardian connectionGuardian;

        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO, string> LobbyCreated;
        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO> PlayerJoined;
        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO> PlayerLeft;
        public event Action<string> GameStartedEvent;
        public event Action<string, bool> PlayerReadyEvent;
        public event Action<string, string> ConnectionError;
        public event Action<List<ArchsVsDinosClient.DTO.LobbyPlayerDTO>> PlayerListUpdated;

        public LobbyServiceClient()
        {
            var synchronizationContext = SynchronizationContext.Current;
            lobbyCallbackManager = new LobbyCallbackManager();

            lobbyCallbackManager.OnJoinedLobby += (playerDto) => PlayerJoined?.Invoke(playerDto);
            lobbyCallbackManager.OnPlayerLeftLobby += (playerDto) => PlayerLeft?.Invoke(playerDto);
            lobbyCallbackManager.OnPlayerListUpdated += (playerList) => PlayerListUpdated?.Invoke(playerList);
            lobbyCallbackManager.OnPlayerReady += (nickname, isReady) => PlayerReadyEvent?.Invoke(nickname, isReady);
            lobbyCallbackManager.OnGameStart += () => GameStartedEvent?.Invoke("");

            var instanceContext = new InstanceContext(lobbyCallbackManager);
            if (synchronizationContext != null)
            {
                instanceContext.SynchronizationContext = synchronizationContext;
            }

            lobbyManagerClient = new LobbyManagerClient(instanceContext);

            connectionGuardian = new WcfConnectionGuardian(
                onError: (title, message) => ConnectionError?.Invoke(title, message),
                logger: new Logger()
            );
            connectionGuardian.MonitorClientState(lobbyManagerClient);
        }

        public async Task<MatchCreationResultCode> CreateLobbyAsync(ArchsVsDinosClient.DTO.UserAccountDTO userAccount)
        {
            var matchSettings = new ArchsVsDinosClient.LobbyService.MatchSettings
            {
                HostNickname = UserSession.Instance.CurrentUser.Nickname,
                MaxPlayers = 4,
                HostUserId = UserSession.Instance.CurrentUser.IdUser
            };

            try
            {
                var response = await Task.Run(() => lobbyManagerClient.CreateLobby(matchSettings));

                if (response.Success)
                {
                    UserSession.Instance.CurrentMatchCode = response.LobbyCode;
                }
                return response.ResultCode;
            }
            catch (Exception)
            {
                return MatchCreationResultCode.MatchCreation_UnexpectedError;
            }
        }

        public async Task<JoinMatchResultCode> JoinLobbyAsync(ArchsVsDinosClient.DTO.UserAccountDTO userAccount, string matchCode)
        {
            try
            {
                var matchJoinResponse = await Task.Run(() => lobbyManagerClient.JoinLobby(
                    matchCode,
                    UserSession.Instance.CurrentPlayer.IdPlayer,
                    UserSession.Instance.CurrentUser.Nickname
                ));

                return matchJoinResponse.ResultCode;
            }
            catch (Exception)
            {
                return JoinMatchResultCode.JoinMatch_UnexpectedError;
            }
        }

        public void ConnectToLobby(string matchCode, string nickname)
        {
            Task ignoredTask = connectionGuardian.ExecuteAsync(async () =>
            {
                await Task.Run(() => lobbyManagerClient.ConnectToLobby(matchCode, nickname));
            });
        }

        public void LeaveLobby(string username)
        {
            var currentMatchCode = UserSession.Instance.CurrentMatchCode;
            Task ignoredTask = connectionGuardian.ExecuteAsync(async () =>
            {
                await Task.Run(() => lobbyManagerClient.DisconnectFromLobby(currentMatchCode, username));
            });
        }

        public void StartGame(string matchCode)
        {
            Task ignoredTask = connectionGuardian.ExecuteAsync(async () =>
            {
                await Task.Run(() => lobbyManagerClient.StartGame(matchCode, UserSession.Instance.CurrentUser.IdUser));
            });
        }

        public async Task<bool> SendLobbyInviteByEmail(string email, string matchCode, string senderUsername)
        {
            try
            {
                string[] guestsList = new string[] { email };
                return await Task.Run(() => lobbyManagerClient.SendInvitations(matchCode, senderUsername, guestsList));
            }
            catch
            {
                return false;
            }
        }
    }
}