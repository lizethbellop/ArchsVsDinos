using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Lobby;
using ArchsVsDinosServer.Services.Interfaces;
using ArchsVsDinosServer.Utils;
using ArchsVsDinosServer.Wrappers;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Response;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class LobbyManager : ILobbyManager
    {
        private readonly ILobbyLogic lobbyLogic;
        private readonly ILoggerHelper logger;
        public LobbyManager()
        {
            logger = ServiceContext.Logger;
            lobbyLogic = ServiceContext.LobbyLogic;
        }
        
        public async Task<MatchCreationResponse> CreateLobby(MatchSettings settings)
        {
            return await lobbyLogic.CreateLobby(settings);
        }

        public async Task<MatchJoinResponse> JoinLobby(JoinLobbyRequest request)
        {
            return await lobbyLogic.JoinLobby(request);
        }

        public async Task<bool> SendInvitations(string lobbyCode, string sender, List<string> guests)
        {
            return await ServiceContext.InvitationSender.SendInvitation(lobbyCode, sender, guests);
        }
 

        public void ConnectToLobby(string lobbyCode, string nickname)
        {
            if (string.IsNullOrWhiteSpace(lobbyCode) || string.IsNullOrWhiteSpace(nickname))
            {
                return;
            }

            if (OperationContext.Current == null)
            {
                logger.LogWarning("ConnectToLobby called without OperationContext.");
                return;
            }

            try
            {
                var callback =
                    OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();

                lobbyLogic.ConnectPlayer(lobbyCode, nickname);
            }
            catch (CommunicationException ex)
            {
                logger.LogWarning($"WCF communication error while connecting {nickname} - {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning($"Timeout while connecting {nickname} to lobby {lobbyCode} - {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                logger.LogError($"Invalid data while connecting to lobby: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                logger.LogError("Error registering lobby connection.", ex);
            }
        }

        public void DisconnectFromLobby(string lobbyCode, string nickname)
        {
            if (string.IsNullOrWhiteSpace(lobbyCode) || string.IsNullOrWhiteSpace(nickname))
            {
                return;
            }

            try
            {
                lobbyLogic.DisconnectPlayer(lobbyCode, nickname);
            }
            catch (CommunicationException ex)
            {
                logger.LogWarning($"Communication error disconnecting {nickname} - {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning($"Timeout disconnecting {nickname} from lobby {lobbyCode} - {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError($"Invalid disconnect operation: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                logger.LogInfo($"Unexpected error in DisconnectFromLobby - {ex.Message}");
            }
        }


        public void SetReadyStatus(string lobbyCode, string nickname, bool isReady)
        {
            if (string.IsNullOrWhiteSpace(lobbyCode) || string.IsNullOrWhiteSpace(nickname))
            {
                return;
            }

            try
            {
                lobbyLogic.UpdatePlayerReadyStatus(lobbyCode, nickname, isReady);
            }
            catch (ArgumentException ex)
            {
                logger.LogError($"Invalid ready state update: {ex.Message}", ex);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError($"Ready state ignored: {ex.Message}", ex);
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning($"Timeout updating ready state for {nickname} - {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError("Unexpected error in SetReadyStatus.", ex);
            }
        }


        public void StartGame(string lobbyCode, int userId)
        {
            if (string.IsNullOrWhiteSpace(lobbyCode))
            {
                return;
            }

            try
            {
                lobbyLogic.EvaluateGameStart(lobbyCode, userId);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning($"Game start rejected: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                logger.LogError($"Invalid start game request: {ex.Message}", ex);
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning($"Timeout starting game in lobby {lobbyCode} - {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError("Unexpected error starting game.", ex);
            }
        }

        public void KickPlayer(string lobbyCode, int hostUserId, string targetNickname)
        {
            if (string.IsNullOrWhiteSpace(lobbyCode) || string.IsNullOrWhiteSpace(targetNickname))
            {
                return;
            }

            try
            {
                lobbyLogic.KickPlayer(lobbyCode, hostUserId, targetNickname);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning($"Unauthorized kick attempt: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                logger.LogError($"Invalid kick request: {ex.Message}", ex);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning($"Kick operation failed: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning($"Timeout kicking player {targetNickname} - {ex.Message}");
            }
            catch (CommunicationException ex)
            {
                logger.LogWarning($"Communication error kicking player: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError("Unexpected error in KickPlayer.", ex);
            }
        }

    }
}
