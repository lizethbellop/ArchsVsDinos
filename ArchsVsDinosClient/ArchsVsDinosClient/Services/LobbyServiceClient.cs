using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Logging;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.Services
{
    public class LobbyServiceClient : ILobbyServiceClient
    {
        private LobbyManagerClient lobbyManagerClient;
        private readonly LobbyCallbackManager lobbyCallbackManager;
        private readonly WcfConnectionGuardian connectionGuardian;
        private GameConnectionTimer connectionTimer;

        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO, string> LobbyCreated;
        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO> PlayerJoined;
        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO> PlayerLeft;
        public event Action<string> GameStartedEvent;
        public event Action<string, bool> PlayerReadyEvent;
        public event Action<string, string> ConnectionError;
        public event Action<List<ArchsVsDinosClient.DTO.LobbyPlayerDTO>> PlayerListUpdated;
        public event Action<string, string> PlayerKickedEvent;
        public event Action<LobbyInvitationDTO> LobbyInvitationReceived;
        public event Action ConnectionLost;

        public LobbyServiceClient()
        {
            var synchronizationContext = SynchronizationContext.Current;
            lobbyCallbackManager = new LobbyCallbackManager();

            lobbyCallbackManager.OnJoinedLobby += (playerDto) => PlayerJoined?.Invoke(playerDto);
            lobbyCallbackManager.OnPlayerLeftLobby += (playerDto) => PlayerLeft?.Invoke(playerDto);
            lobbyCallbackManager.OnPlayerListUpdated += (playerList) => PlayerListUpdated?.Invoke(playerList);
            lobbyCallbackManager.OnPlayerReady += (nickname, isReady) => PlayerReadyEvent?.Invoke(nickname, isReady);
            lobbyCallbackManager.OnGameStart += () => GameStartedEvent?.Invoke("");
            lobbyCallbackManager.OnLobbyInvitationReceived += (invitation) => LobbyInvitationReceived?.Invoke(invitation);

            lobbyCallbackManager.OnPlayerKicked += (nickname, reason) =>
            {
                if (PlayerKickedEvent != null) PlayerKickedEvent(nickname, reason);
            };

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
                HostUsername = UserSession.Instance.CurrentUser.Username,
                MaxPlayers = 4,
                HostUserId = UserSession.Instance.CurrentUser.IdUser
            };

            try
            {
                EnsureClientIsUsable();

                var response = await connectionGuardian.ExecuteWithThrowAsync(() =>
                    Task.FromResult(lobbyManagerClient.CreateLobby(matchSettings))
                );

                if (response.Success)
                {
                    UserSession.Instance.CurrentMatchCode = response.LobbyCode;
                    await ConnectToLobbyAsync(response.LobbyCode, matchSettings.HostNickname);
                }

                return response.ResultCode;
            }
            catch (EndpointNotFoundException endpointEx)
            {
                ResetLobbyClient();
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerNotFound);
                Debug.WriteLine($"[LOBBY CLIENT] EndpointNotFoundException: {endpointEx.Message}");
                return MatchCreationResultCode.MatchCreation_ServerBusy;
            }
            catch (FaultException faultEx)
            {
                ResetLobbyClient();
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerError);
                Debug.WriteLine($"[LOBBY CLIENT] FaultException: {faultEx.Message}");
                return MatchCreationResultCode.MatchCreation_UnexpectedError;
            }
            catch (CommunicationException commEx)
            {
                ResetLobbyClient();
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerUnavailable);
                Debug.WriteLine($"[LOBBY CLIENT] CommunicationException: {commEx.Message}");
                return MatchCreationResultCode.MatchCreation_Failure;
            }
            catch (TimeoutException timeoutEx)
            {
                ResetLobbyClient();
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerTimeout);
                Debug.WriteLine($"[LOBBY CLIENT] TimeoutException: {timeoutEx.Message}");
                return MatchCreationResultCode.MatchCreation_Timeout;
            }
            catch (ObjectDisposedException objEx)
            {
                ResetLobbyClient();
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerError);
                Debug.WriteLine($"[LOBBY CLIENT] ObjectDisposedException: {objEx.Message}");
                return MatchCreationResultCode.MatchCreation_UnexpectedError;
            }
            catch (InvalidOperationException invEx)
            {
                ResetLobbyClient();
                ConnectionError?.Invoke(Lang.GlobalUnexpectedError, Lang.GlobalServerError);
                Debug.WriteLine($"[LOBBY CLIENT] InvalidOperationException: {invEx.Message}");
                return MatchCreationResultCode.MatchCreation_UnexpectedError;
            }
        }

        public async Task<JoinMatchResultCode> JoinLobbyAsync(ArchsVsDinosClient.DTO.UserAccountDTO userAccount, string matchCode)
        {
            try
            {
                EnsureClientIsUsable();
                var request = new ArchsVsDinosClient.LobbyService.JoinLobbyRequest
                {
                    LobbyCode = matchCode,
                    UserId = userAccount.IdPlayer,
                    Nickname = userAccount.Nickname,
                    Username = userAccount.Username
                };

                var matchJoinResponse = await connectionGuardian.ExecuteWithThrowAsync(() =>
                    Task.FromResult(lobbyManagerClient.JoinLobby(request)));

                return matchJoinResponse.ResultCode;
            }
            catch (EndpointNotFoundException endpointEx)
            {
                ResetLobbyClient();
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerNotFound);
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby EndpointNotFoundException: {endpointEx.Message}");
                return JoinMatchResultCode.JoinMatch_UnexpectedError;
            }
            catch (FaultException<string> faultEx)
            {
                ResetLobbyClient();
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerError);
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby FaultException<string>: {faultEx.Detail}");
                return JoinMatchResultCode.JoinMatch_LobbyFull;
            }
            catch (FaultException faultEx)
            {
                ResetLobbyClient();
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerError);
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby FaultException: {faultEx.Message}");
                return JoinMatchResultCode.JoinMatch_UnexpectedError;
            }
            catch (CommunicationException commEx)
            {
                ResetLobbyClient();
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerUnavailable);
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby CommunicationException: {commEx.Message}");
                return JoinMatchResultCode.JoinMatch_UnexpectedError;
            }
            catch (TimeoutException timeoutEx)
            {
                ResetLobbyClient();
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerTimeout);
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby TimeoutException: {timeoutEx.Message}");
                return JoinMatchResultCode.JoinMatch_Timeout;
            }
            catch (ObjectDisposedException objEx)
            {
                ResetLobbyClient();
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerError);
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby ObjectDisposedException: {objEx.Message}");
                return JoinMatchResultCode.JoinMatch_UnexpectedError;
            }
            catch (InvalidOperationException invEx)
            {
                ResetLobbyClient();
                ConnectionError?.Invoke(Lang.GlobalUnexpectedError, Lang.GlobalServerError);
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby InvalidOperationException: {invEx.Message}");
                return JoinMatchResultCode.JoinMatch_UnexpectedError;
            }
        }

        public async Task<bool> ConnectToLobbyAsync(string matchCode, string nickname)
        {
            return await connectionGuardian.ExecuteAsync(async () =>
            {
                await Task.Run(() => lobbyManagerClient.ConnectToLobby(matchCode, nickname));
            }, operationName: "ConnectToLobby");
        }


        public void LeaveLobby(string username)
        {
            var currentMatchCode = UserSession.Instance.CurrentMatchCode;

            if (lobbyManagerClient != null)
            {
                var state = ((ICommunicationObject)lobbyManagerClient).State;

                Debug.WriteLine($"[LOBBY CLIENT] LeaveLobby - Estado del cliente: {state}");

                if (state == CommunicationState.Faulted ||
                    state == CommunicationState.Closed)
                {
                    Debug.WriteLine($"[LOBBY CLIENT] ⚠️ No se puede desconectar: conexión en estado {state}");
                    Debug.WriteLine($"[LOBBY CLIENT] Regenerando proxy...");
                    ResetLobbyClient();
                    return;
                }
            }

            Task ignoredTask = connectionGuardian.ExecuteAsync(
                operation: async () =>
                {
                    await Task.Run(() => lobbyManagerClient.DisconnectFromLobby(currentMatchCode, username));
                },
                operationName: "DisconnectFromLobby",
                suppressErrors: true
            );
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

                var result = await connectionGuardian.ExecuteAsync(async () =>
                {
                    return await Task.Run(() => lobbyManagerClient.SendInvitations(matchCode, senderUsername, guestsList));
                });

                return result;
            }
            catch (EndpointNotFoundException endpointEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite EndpointNotFoundException: {endpointEx.Message}");
                MessageBox.Show(Lang.GlobalServerNotFound, Lang.GlobalError, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (FaultException faultEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite FaultException: {faultEx.Message}");
                MessageBox.Show(Lang.GlobalServerError, Lang.GlobalError, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (CommunicationException commEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite CommunicationException: {commEx.Message}");
                MessageBox.Show(Lang.GlobalServerUnavailable, Lang.GlobalError, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (TimeoutException timeoutEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite TimeoutException: {timeoutEx.Message}");
                MessageBox.Show(Lang.GlobalServerTimeout, Lang.GlobalError, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (FormatException formatEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite FormatException: {formatEx.Message}");
                MessageBox.Show(Lang.Register_InvalidEmail, Lang.GlobalError, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (ArgumentNullException argEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite ArgumentNullException: {argEx.Message}");
                MessageBox.Show(Lang.Authentication_UnexpectedError, Lang.GlobalError, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        public void KickPlayer(string lobbyCode, int hostUserId, string targetNickname)
        {
            Task ignoredTask = connectionGuardian.ExecuteAsync(async () =>
            {
                await Task.Run(() => lobbyManagerClient.KickPlayer(lobbyCode, hostUserId, targetNickname));
            });
        }

        public async Task<bool> SendLobbyInviteToFriendAsync(string lobbyCode, string senderNickname, string targetUsername)
        {
            try
            {
                var result = await connectionGuardian.ExecuteAsync(async () =>
                {
                    return await lobbyManagerClient.SendLobbyInviteToFriendAsync(lobbyCode, senderNickname, targetUsername);
                });

                return result;
            }
            catch (EndpointNotFoundException endpointEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendLobbyInviteToFriend EndpointNotFoundException: {endpointEx.Message}");
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerNotFound);
                return false;
            }
            catch (FaultException faultEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendLobbyInviteToFriend FaultException: {faultEx.Message}");
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerError);
                return false;
            }
            catch (CommunicationException commEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendLobbyInviteToFriend CommunicationException: {commEx.Message}");
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerUnavailable);
                return false;
            }
            catch (TimeoutException timeoutEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendLobbyInviteToFriend TimeoutException: {timeoutEx.Message}");
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerTimeout);
                return false;
            }
            catch (ObjectDisposedException objEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendLobbyInviteToFriend ObjectDisposedException: {objEx.Message}");
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerError);
                return false;
            }
            catch (InvalidOperationException invEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendLobbyInviteToFriend InvalidOperationException: {invEx.Message}");
                ConnectionError?.Invoke(Lang.GlobalUnexpectedError, Lang.GlobalServerError);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendLobbyInviteToFriend Exception: {ex.Message}");
                ConnectionError?.Invoke(Lang.GlobalUnexpectedError, Lang.GlobalServerError);
                return false;
            }
        }

        private void EnsureClientIsUsable()
        {
            if (lobbyManagerClient == null)
            {
                ResetLobbyClient();
                return;
            }

            var state = ((ICommunicationObject)lobbyManagerClient).State;

            if (state == CommunicationState.Faulted ||
                state == CommunicationState.Closed)
            {
                ResetLobbyClient();
            }
        }

        private void ResetLobbyClient()
        {
            if (lobbyManagerClient is ICommunicationObject comm)
            {
                try
                {
                    if (comm.State == CommunicationState.Faulted)
                        comm.Abort();
                    else
                        comm.Close();
                }
                catch
                {
                    comm.Abort();
                }
            }

            var synchronizationContext = SynchronizationContext.Current;
            var instanceContext = new InstanceContext(lobbyCallbackManager);

            if (synchronizationContext != null)
                instanceContext.SynchronizationContext = synchronizationContext;

            lobbyManagerClient = new LobbyManagerClient(instanceContext);
            connectionGuardian.MonitorClientState(lobbyManagerClient);
        }

        public async Task<bool> TryReconnectToLobbyAsync(string matchCode, string nickname)
        {
            try
            {
                Debug.WriteLine($"[LOBBY CLIENT] Trying to reconnect to the lobby {matchCode}...");

                connectionTimer?.Stop();

                EnsureClientIsUsable();

                bool connected = await connectionGuardian.ExecuteAsync(
                    async () =>
                    {
                        await Task.Run(() => lobbyManagerClient.ConnectToLobby(matchCode, nickname));
                    },
                    operationName: "ReconnectToLobby"
                );

                if (connected)
                {
                    connectionTimer?.Start();
                    Debug.WriteLine($"[LOBBY CLIENT] Reconnection to the lobby successfully {matchCode}");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"[LOBBY CLIENT] Failing connecting to the lobby");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY CLIENT] Error in connection {ex.Message}");
                return false;
            }
        }

        public void StartConnectionMonitoring(int timeoutSeconds)
        {
            if (connectionTimer != null)
            {
                connectionTimer.Dispose();
            }

            connectionTimer = new GameConnectionTimer(
                timeoutSeconds,
                onTimeout: () => ConnectionLost?.Invoke()
            );

            lobbyCallbackManager.SetConnectionTimer(connectionTimer);

            connectionTimer.Start();
            System.Diagnostics.Debug.WriteLine($"[LOBBY TIMER] Started with {timeoutSeconds}s timeout");
        }

        public void StopConnectionMonitoring()
        {
            connectionTimer?.Stop();
            System.Diagnostics.Debug.WriteLine("[LOBBY TIMER] Stopped");
        }
    }
}
