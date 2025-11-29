using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Logging;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.Services
{
    public class LobbyServiceClient : ILobbyServiceClient
    {
        private readonly LobbyManagerClient lobbyManagerClient;
        private readonly LobbyCallbackManager lobbyCallbackManager;
        private readonly WcfConnectionGuardian connectionGuardian;
        private readonly SynchronizationContext synchronizationContext;
        private bool isDisposed;

        public event Action<LobbyPlayerDTO, string> LobbyCreated;
        public event Action<LobbyPlayerDTO> PlayerJoined;
        public event Action<LobbyPlayerDTO> PlayerLeft;
        public event Action<LobbyPlayerDTO> PlayerExpelled;
        public event Action<string> LobbyCancelled;
        public event Action<string, List<LobbyPlayerDTO>> GameStartedEvent;
        public event Action<string, string> ConnectionError;

        public LobbyServiceClient()
        {
            synchronizationContext = SynchronizationContext.Current;
            lobbyCallbackManager = new LobbyCallbackManager();

            lobbyCallbackManager.OnCreatedLobby += (player, code) => LobbyCreated?.Invoke(player, code);
            lobbyCallbackManager.OnJoinedLobby += (player) => PlayerJoined?.Invoke(player);
            lobbyCallbackManager.OnPlayerLeftLobby += (player) => PlayerLeft?.Invoke(player);
            lobbyCallbackManager.OnPlayerExpelled += (player) => PlayerExpelled?.Invoke(player);
            lobbyCallbackManager.OnLobbyCancelled += (code) => LobbyCancelled?.Invoke(code);
            lobbyCallbackManager.OnGameStarted += (matchCode, players) => GameStartedEvent?.Invoke(matchCode, players);

            var instanceContext = new InstanceContext(lobbyCallbackManager);
            instanceContext.SynchronizationContext = synchronizationContext;

            lobbyManagerClient = new LobbyManagerClient(instanceContext);

            connectionGuardian = new WcfConnectionGuardian(
                onError: (title, message) => ConnectionError?.Invoke(title, message),
                logger: new Logger()
            );
            connectionGuardian.MonitorClientState(lobbyManagerClient);
        }

        public void CreateLobby(UserAccountDTO userAccount)
        {
            Task ignoredTask = connectionGuardian.ExecuteAsync(
                async () => await Task.Run(() => lobbyManagerClient.CreateLobby(userAccount))
            );
        }

        public LobbyResultCode JoinLobby(UserAccountDTO userAccount, string matchCode)
        {
            return lobbyManagerClient.JoinLobby(userAccount, matchCode);
        }

        public void LeaveLobby(string username)
        {
            Task ignoredTask = connectionGuardian.ExecuteAsync(
                async () => await Task.Run(() => lobbyManagerClient.LeaveLobby(username))
            );
        }

        public void ExpelPlayer(string targetUsername, string hostUsername)
        {
            Task ignoredTask = connectionGuardian.ExecuteAsync(
                async () => await Task.Run(() => lobbyManagerClient.ExpelPlayerLobby(hostUsername, targetUsername))
            );
        }

        public void CancellLobby(string matchCode, string usernameRequester)
        {
            Task ignoredTask = connectionGuardian.ExecuteAsync(
                async () => await Task.Run(() => lobbyManagerClient.CancelLobby(matchCode, usernameRequester))
            );
        }

        public void StartGame(string matchCode, string hostUsername)
        {
            Task ignoredTask = connectionGuardian.ExecuteAsync(
                async () => await Task.Run(() => lobbyManagerClient.StartGame(matchCode, hostUsername))
            );
        }

        public LobbyResultCode SendLobbyInviteByEmail(string email, string matchCode, string senderUsername)
        {
            try
            {
                return lobbyManagerClient.InviteByEmailToLobby(email, matchCode, senderUsername);
            }
            catch (CommunicationException ex) 
            {
                var logger = new Logger();
                logger.LogError(Lang.Lobby_ErrorSendingEmail);

                return LobbyResultCode.Lobby_ConnectionError;
            }
        }

        public void Dispose()
        {
            if (isDisposed) return;

            if (lobbyCallbackManager != null)
            {
                lobbyCallbackManager.OnCreatedLobby -= (player, code) => LobbyCreated?.Invoke(player, code);
                lobbyCallbackManager.OnJoinedLobby -= (player) => PlayerJoined?.Invoke(player);
                lobbyCallbackManager.OnPlayerLeftLobby -= (player) => PlayerLeft?.Invoke(player);
                lobbyCallbackManager.OnPlayerExpelled -= (player) => PlayerExpelled?.Invoke(player);
                lobbyCallbackManager.OnLobbyCancelled -= (code) => LobbyCancelled?.Invoke(code);
            }

            try
            {
                if (lobbyManagerClient?.State == CommunicationState.Opened)
                {
                    lobbyManagerClient.Close();
                }
                else if (lobbyManagerClient?.State == CommunicationState.Faulted)
                {
                    lobbyManagerClient.Abort();
                }
            }
            catch
            {
                lobbyManagerClient?.Abort();
            }

            isDisposed = true;
        }
    }
}