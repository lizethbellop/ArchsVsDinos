using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Logging;
using ArchsVsDinosClient.Models;
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
        public event Action<string, string> PlayerKickedEvent;

        public LobbyServiceClient()
        {
            var synchronizationContext = SynchronizationContext.Current;
            lobbyCallbackManager = new LobbyCallbackManager();

            lobbyCallbackManager.OnJoinedLobby += (playerDto) => PlayerJoined?.Invoke(playerDto);
            lobbyCallbackManager.OnPlayerLeftLobby += (playerDto) => PlayerLeft?.Invoke(playerDto);
            lobbyCallbackManager.OnPlayerListUpdated += (playerList) => PlayerListUpdated?.Invoke(playerList);
            lobbyCallbackManager.OnPlayerReady += (nickname, isReady) => PlayerReadyEvent?.Invoke(nickname, isReady);
            lobbyCallbackManager.OnGameStart += () => GameStartedEvent?.Invoke("");

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
                MaxPlayers = 4,
                HostUserId = UserSession.Instance.CurrentUser.IdUser
            };

            try
            {
                var response = await connectionGuardian.ExecuteAsync(async () =>
                {
                    return await Task.Run(() => lobbyManagerClient.CreateLobby(matchSettings));
                });

                if (response.Success)
                {
                    UserSession.Instance.CurrentMatchCode = response.LobbyCode;
                    await ConnectToLobbyAsync(response.LobbyCode, matchSettings.HostNickname);
                }

                return response.ResultCode;
            }
            catch (EndpointNotFoundException endpointEx)
            {
                ConnectionError?.Invoke("Servidor no disponible", "No se encontró el servidor del lobby.");
                Debug.WriteLine($"[LOBBY CLIENT] EndpointNotFoundException: {endpointEx.Message}");
                return MatchCreationResultCode.MatchCreation_ServerBusy;
            }
            catch (FaultException faultEx)
            {
                ConnectionError?.Invoke("Error del servidor", $"El servidor reportó un error: {faultEx.Message}");
                Debug.WriteLine($"[LOBBY CLIENT] FaultException: {faultEx.Message}");
                return MatchCreationResultCode.MatchCreation_UnexpectedError;
            }
            catch (CommunicationException commEx)
            {
                ConnectionError?.Invoke("Error de comunicación", "No se pudo conectar con el servidor del lobby.");
                Debug.WriteLine($"[LOBBY CLIENT] CommunicationException: {commEx.Message}");
                return MatchCreationResultCode.MatchCreation_Failure;
            }
            catch (TimeoutException timeoutEx)
            {
                ConnectionError?.Invoke("Tiempo agotado", "El servidor no respondió a tiempo.");
                Debug.WriteLine($"[LOBBY CLIENT] TimeoutException: {timeoutEx.Message}");
                return MatchCreationResultCode.MatchCreation_Timeout;
            }
            catch (ObjectDisposedException objEx)
            {
                ConnectionError?.Invoke("Conexión cerrada", "La conexión con el servidor fue cerrada inesperadamente.");
                Debug.WriteLine($"[LOBBY CLIENT] ObjectDisposedException: {objEx.Message}");
                return MatchCreationResultCode.MatchCreation_UnexpectedError;
            }
            catch (InvalidOperationException invEx)
            {
                ConnectionError?.Invoke("Estado inválido", "La conexión con el servidor está en un estado inválido.");
                Debug.WriteLine($"[LOBBY CLIENT] InvalidOperationException: {invEx.Message}");
                return MatchCreationResultCode.MatchCreation_UnexpectedError;
            }
        }

        public async Task<JoinMatchResultCode> JoinLobbyAsync(ArchsVsDinosClient.DTO.UserAccountDTO userAccount, string matchCode)
        {
            try
            {
                var matchJoinResponse = await connectionGuardian.ExecuteAsync(async () =>
                {
                    return await Task.Run(() => lobbyManagerClient.JoinLobby(
                        matchCode,
                        userAccount.IdPlayer,
                        userAccount.Nickname
                    ));
                });

                return matchJoinResponse.ResultCode;
            }
            catch (EndpointNotFoundException endpointEx)
            {
                ConnectionError?.Invoke("Servidor no disponible", "No se pudo encontrar el servidor del lobby.");
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby EndpointNotFoundException: {endpointEx.Message}");
                return JoinMatchResultCode.JoinMatch_UnexpectedError;
            }
            catch (FaultException<string> faultEx)
            {
                ConnectionError?.Invoke("Error del lobby", faultEx.Detail);
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby FaultException<string>: {faultEx.Detail}");
                return JoinMatchResultCode.JoinMatch_LobbyFull; 
            }
            catch (FaultException faultEx)
            {
                ConnectionError?.Invoke("Error del servidor", $"Error al unirse: {faultEx.Message}");
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby FaultException: {faultEx.Message}");
                return JoinMatchResultCode.JoinMatch_UnexpectedError;
            }
            catch (CommunicationException commEx)
            {
                ConnectionError?.Invoke("Error de comunicación", "No se pudo unir al lobby. Verifica tu conexión.");
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby CommunicationException: {commEx.Message}");
                return JoinMatchResultCode.JoinMatch_UnexpectedError;
            }
            catch (TimeoutException timeoutEx)
            {
                ConnectionError?.Invoke("Tiempo agotado", "El servidor tardó demasiado en responder.");
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby TimeoutException: {timeoutEx.Message}");
                return JoinMatchResultCode.JoinMatch_Timeout;
            }
            catch (ObjectDisposedException objEx)
            {
                ConnectionError?.Invoke("Conexión cerrada", "La conexión fue cerrada antes de unirse al lobby.");
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby ObjectDisposedException: {objEx.Message}");
                return JoinMatchResultCode.JoinMatch_UnexpectedError;
            }
            catch (InvalidOperationException invEx)
            {
                ConnectionError?.Invoke("Operación inválida", "No se puede realizar la operación en este momento.");
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby InvalidOperationException: {invEx.Message}");
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

        public async Task ConnectToLobbyAsync(string matchCode, string nickname)
        {
            Debug.WriteLine($"[CLIENT] ConnectToLobbyAsync: matchCode={matchCode}, nickname={nickname}");

            await connectionGuardian.ExecuteAsync(async () =>
            {
                Debug.WriteLine($"[CLIENT] Calling server ConnectToLobby...");
                await Task.Run(() => lobbyManagerClient.ConnectToLobby(matchCode, nickname));
                Debug.WriteLine($"[CLIENT] Server call completed");
            });

            Debug.WriteLine($"[CLIENT] ConnectToLobbyAsync finished");
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

                var result = await connectionGuardian.ExecuteAsync(async () =>
                {
                    return await Task.Run(() => lobbyManagerClient.SendInvitations(matchCode, senderUsername, guestsList));
                });

                return result;
            }
            catch (EndpointNotFoundException endpointEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite EndpointNotFoundException: {endpointEx.Message}");
                MessageBox.Show("El servidor de invitaciones no está disponible.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (FaultException faultEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite FaultException: {faultEx.Message}");
                MessageBox.Show($"Error del servidor: {faultEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (CommunicationException commEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite CommunicationException: {commEx.Message}");
                MessageBox.Show("No se pudo enviar la invitación. Verifica tu conexión.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (TimeoutException timeoutEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite TimeoutException: {timeoutEx.Message}");
                MessageBox.Show("El servidor tardó demasiado en responder.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (FormatException formatEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite FormatException: {formatEx.Message}");
                MessageBox.Show("El formato del email es inválido.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (ArgumentNullException argEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite ArgumentNullException: {argEx.Message}");
                MessageBox.Show("Datos incompletos para enviar la invitación.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
    }
}