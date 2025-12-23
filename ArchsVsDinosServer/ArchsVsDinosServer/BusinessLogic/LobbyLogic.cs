using ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Interfaces.Lobby;
using ArchsVsDinosServer.Model;
using Contracts.DTO.Result_Codes;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Response;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic
{
    public class LobbyLogic : ILobbyLogic
    {
        private readonly LobbyCoreContext core;
        private readonly ILoggerHelper logger;
        private readonly IGameLogic gameLogic;
        private readonly IInvitationSendHelper invitationSendHelper;

        public LobbyLogic(
        LobbyCoreContext core,
        ILoggerHelper logger,
        IGameLogic gameLogic,
        IInvitationSendHelper invitationSendHelper)
        {
            this.core = core;
            this.logger = logger;
            this.gameLogic = gameLogic;
            this.invitationSendHelper = invitationSendHelper;
        }
        public Task<MatchCreationResponse> CreateLobby(MatchSettings settings)
        {
            if (settings == null)
            {
                return Task.FromResult(new MatchCreationResponse
                {
                    Success = false,
                    ResultCode = MatchCreationResultCode.MatchCreation_InvalidParameters
                });
            }

            try
            {
                core.Validation.ValidateCreateLobby(settings);
                var lobbyCode = core.CodeGenerator.GenerateLobbyCode(code => core.Session.LobbyExists(code));
                var lobbyData = new ActiveLobbyData(lobbyCode, settings);

                // ✅ AGREGAR AL HOST AUTOMÁTICAMENTE
                lobbyData.AddPlayer(settings.HostUserId, settings.HostNickname);

                core.Session.CreateLobby(lobbyCode, lobbyData);
                logger.LogInfo($"Lobby {lobbyCode} created by {settings.HostNickname}");

                return Task.FromResult(new MatchCreationResponse
                {
                    Success = true,
                    LobbyCode = lobbyCode,
                    ResultCode = MatchCreationResultCode.MatchCreation_Success
                });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError("Failed to generate lobby code.", ex);
                return Task.FromResult(new MatchCreationResponse
                {
                    Success = false,
                    ResultCode = MatchCreationResultCode.MatchCreation_ServerBusy
                });
            }
            catch (ArgumentException ex)
            {
                logger.LogError($"CreateLobby failed, invalid settings: {ex.Message}", ex);
                return Task.FromResult(new MatchCreationResponse
                {
                    Success = false,
                    ResultCode = MatchCreationResultCode.MatchCreation_InvalidSettings
                });
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning($"CreateLobby failed, timeout: {ex.Message}");
                return Task.FromResult(new MatchCreationResponse
                {
                    Success = false,
                    ResultCode = MatchCreationResultCode.MatchCreation_Timeout
                });
            }
        }

        public Task<MatchJoinResponse> JoinLobby(string lobbyCode, int userId, string nickname)
        {
            if (string.IsNullOrWhiteSpace(lobbyCode) || string.IsNullOrWhiteSpace(nickname))
            {
                return Task.FromResult(new MatchJoinResponse
                {
                    Success = false,
                    ResultCode = JoinMatchResultCode.JoinMatch_InvalidParameters
                });
            }

            try
            {
                core.Validation.ValidateJoinLobby(lobbyCode, nickname);
                var lobby = core.Session.GetLobby(lobbyCode);

                if(lobby == null)
                {
                    return Task.FromResult(new MatchJoinResponse
                    {
                        Success = false,
                        ResultCode = JoinMatchResultCode.JoinMatch_LobbyNotFound
                    });
                }

                int finalUserId = userId;
                if (userId == 0)
                {
                    finalUserId = -Math.Abs(nickname.GetHashCode() ^ DateTime.UtcNow.Ticks.GetHashCode());
                    logger.LogInfo($"Guest '{nickname}' assigned temporary userId: {finalUserId}");
                }

                var joined = lobby.AddPlayer(finalUserId, nickname);

                if (!joined)
                {
                    return Task.FromResult(new MatchJoinResponse
                    {
                        Success = false,
                        ResultCode = JoinMatchResultCode.JoinMatch_LobbyFull
                    });
                }

                logger.LogInfo($"Player {nickname} joined lobby {lobbyCode}");
                return Task.FromResult(new MatchJoinResponse
                {
                    Success = true,
                    ResultCode = JoinMatchResultCode.JoinMatch_Success,
                    LobbyCode = lobbyCode
                });
            }
            catch (ArgumentException ex)
            {
                logger.LogError($"JoinLobby validation failed: {ex.Message}", ex);

                return Task.FromResult(new MatchJoinResponse
                {
                    Success = false,
                    ResultCode = JoinMatchResultCode.JoinMatch_InvalidSettings
                });
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning($"JoinLobby timeout: {ex.Message}");

                return Task.FromResult(new MatchJoinResponse
                {
                    Success = false,
                    ResultCode = JoinMatchResultCode.JoinMatch_Timeout
                });
            }
        }

        public void ConnectPlayer(string lobbyCode, string playerNickname)
        {
            if (string.IsNullOrWhiteSpace(lobbyCode) || string.IsNullOrWhiteSpace(playerNickname))
            {
                return;
            }

            if (OperationContext.Current == null)
            {
                logger.LogWarning("RegisterConnection called without OperationContext.");
                return;
            }

            try
            {
                var callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();

                if (callback == null)
                {
                    logger.LogWarning("RegisterConnection failed to get callback channel.");
                    return;
                }

                var lobby = core.Session.GetLobby(lobbyCode);

                if (lobby == null)
                {
                    logger.LogWarning($"Player registered connection but lobby {lobbyCode} was not found.");
                    return;
                }

                core.Session.ConnectPlayerCallback(lobbyCode, playerNickname, callback);

                lock (lobby.LobbyLock)
                {
                    var existingPlayer = lobby.Players.FirstOrDefault(p =>
                        p.Nickname.Equals(playerNickname, StringComparison.OrdinalIgnoreCase));

                    if (existingPlayer == null)
                    {
                        logger.LogWarning($"Player {playerNickname} not found in lobby {lobbyCode} players list!");
                        return;
                    }
                }

                try
                {
                    var currentList = MapPlayersToDTOs(lobby);
                    callback.UpdateListOfPlayers(currentList);
                    logger.LogInfo($"Sent initial state to {playerNickname}: {currentList.Length} players");
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Failed to send initial state to {playerNickname}: {ex.Message}");
                }

                core.Session.Broadcast(lobbyCode, cb =>
                {
                    if (cb != callback) // No notificar al que acaba de conectarse
                    {
                        cb.PlayerJoinedLobby(playerNickname);
                    }
                });

                core.Session.Broadcast(lobbyCode, cb => cb.UpdateListOfPlayers(MapPlayersToDTOs(lobby)));
            }
            catch (CommunicationException ex)
            {
                logger.LogWarning($"Communication error registering connection: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning($"Timeout registering connection: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Critical error registering connection: {ex.Message}");
            }
        }

        public Task UpdatePlayerReadyStatus(string lobbyCode, string playerName, bool isReady)
        {
            if (string.IsNullOrWhiteSpace(lobbyCode) || string.IsNullOrWhiteSpace(playerName))
            {
                return Task.CompletedTask;
            }

            var lobby = core.Session.GetLobby(lobbyCode);
            if (lobby == null)
            {
                logger.LogWarning($"UpdatePlayerReadyStatus: Lobby {lobbyCode} not found.");
                return Task.CompletedTask;
            }

            lock (lobby.LobbyLock)
            {
                var player = lobby.Players
                    .FirstOrDefault(p => p.Nickname.Equals(playerName, StringComparison.OrdinalIgnoreCase));

                if (player == null)
                {
                    return Task.CompletedTask;
                }

                player.IsReady = isReady;
            }

            core.Session.Broadcast(
                lobbyCode,
                callback => callback.PlayerReadyStatusChanged(playerName, isReady)
            );

            return Task.CompletedTask;
        }

        public async Task EvaluateGameStart(string lobbyCode, int userId)
        {
            var lobby = core.Session.GetLobby(lobbyCode);
            if (lobby == null)
            {
                logger.LogWarning($"EvaluateGameStart: Lobby {lobbyCode} not found.");
                return;
            }

            if (lobby.HostUserId != userId)
            {
                logger.LogWarning($"User {userId} tried to start game but is not host. Host is {lobby.HostUserId}");
                return;
            }

            bool shouldStart;

            lock (lobby.LobbyLock)
            {
                shouldStart = lobby.Players.Count >= 2;

                logger.LogInfo($"EvaluateGameStart: Lobby {lobbyCode} has {lobby.Players.Count} players");
            }

            if (shouldStart)
            {
                await TryStartingGame(lobbyCode);
            }
            else
            {
                logger.LogInfo($"Game start blocked: need at least 2 players in {lobbyCode}");
            }
        }



        private async Task TryStartingGame(string lobbyCode)
        {
            await Task.Delay(2000);

            var lobby = core.Session.GetLobby(lobbyCode);
            if (lobby == null)
            {
                logger.LogWarning($"TryStartingGame: Lobby {lobbyCode} not found.");
                return;
            }

            List<GamePlayerInitDTO> playersToStart;

            lock (lobby.LobbyLock)
            {
                if (lobby.Players.Count < 2)
                {
                    logger.LogInfo($"Game start cancelled for {lobbyCode}. Not enough players.");
                    return;
                }

                playersToStart = lobby.Players
                    .Select(p => new GamePlayerInitDTO
                    {
                        UserId = p.UserId,
                        Nickname = p.Nickname
                    })
                    .ToList();
            }

            bool created = await gameLogic.InitializeMatch(lobbyCode, playersToStart);

            if (!created)
            {
                logger.LogWarning($"TryStartingGame: Failed to create game for lobby {lobbyCode}.");
                return;
            }

            core.Session.Broadcast(lobbyCode, callback => callback.GameStarting());

            logger.LogInfo($"Game starting for lobby {lobbyCode}.");
        }


        public void DisconnectPlayer(string lobbyCode, string playerNickname)
        {
            if (string.IsNullOrWhiteSpace(lobbyCode) || string.IsNullOrWhiteSpace(playerNickname))
            {
                return;
            }

            try
            {
                var lobby = core.Session.GetLobby(lobbyCode);
                if (lobby == null) return;

                var leavingPlayer = lobby.Players.FirstOrDefault(p =>
                    p.Nickname.Equals(playerNickname, StringComparison.OrdinalIgnoreCase));

                if (leavingPlayer == null) return;

                bool wasHost = (lobby.HostUserId == leavingPlayer.UserId);

                HandlePlayerExit(lobbyCode, playerNickname);
                core.Session.DisconnectPlayerCallback(lobbyCode, playerNickname);

                if (wasHost && lobby.Players.Count > 0)
                {
                    lobby.TransferHostToNextPlayer();
                    var newHost = lobby.Players.First(p => p.UserId == lobby.HostUserId);

                    logger.LogInfo($"Host transferred to {newHost.Nickname} in lobby {lobbyCode}");

                    core.Session.Broadcast(lobbyCode, cb =>
                        cb.UpdateListOfPlayers(MapPlayersToDTOs(lobby)));
                }
                else if (lobby.Players.Count == 0)
                {
                    core.Session.RemoveLobby(lobbyCode);
                    logger.LogInfo($"Lobby {lobbyCode} removed - empty");
                }
            }
            catch (CommunicationException ex)
            {
                logger.LogWarning($"Communication error disconnecting player: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning($"Timeout disconnecting player: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Error disconnecting player: {ex.Message}");
            }
        }

        public async Task<bool> SendInvitations(string lobbyCode, string sender, List<string> guests)
        {
            if (string.IsNullOrWhiteSpace(lobbyCode)
                || string.IsNullOrWhiteSpace(sender)
                || guests == null
                || guests.Count == 0)
            {
                return false;
            }

            try
            {
                core.Validation.ValidateInviteGuests(guests);
                var lobby = core.Session.GetLobby(lobbyCode);
                if (lobby == null)
                {
                    logger.LogWarning($"SendInvitations failed: Lobby {lobbyCode} does not exist.");
                    return false;
                }

                return await invitationSendHelper.SendInvitation(lobbyCode, sender, guests);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning($"SendInvitations validation failed: {ex.Message}");
                return false;
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning($"SendInvitations timeout: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError("Unexpected error sending invitations.", ex);
                return false;
            }
        }

        private void SendInitialState(ILobbyManagerCallback callback, ActiveLobbyData activeLobbyData, string nickname)
        {
            if(callback == null || activeLobbyData == null || string.IsNullOrWhiteSpace(nickname))
            {
                return;
            }

            try
            {
                var players = activeLobbyData.Players;
                callback.UpdateListOfPlayers(MapPlayersToDTOs(activeLobbyData));
            }
            catch (CommunicationException ex)
            {
                logger.LogWarning($"Communication error sending initial state to {nickname}: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning($"Timeout sending initial state to {nickname}: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Critical error sending initial state to {nickname}.", ex);
            }
        }

        private void HandlePlayerExit(string lobbyCode, string nickname)
        {
            var lobby = core.Session.GetLobby(lobbyCode);
            if (lobby == null)
            {
                logger.LogWarning($"HandlePlayerExit: Lobby {lobbyCode} not found.");
                return;
            }

            core.Session.Broadcast(lobbyCode, cb => cb.PlayerLeftLobby(nickname));
            lobby.RemovePlayer(nickname);
            core.Session.Broadcast(lobbyCode, cb => cb.UpdateListOfPlayers(MapPlayersToDTOs(lobby)));

        }

        private LobbyPlayerDTO[] MapPlayersToDTOs(ActiveLobbyData lobby)
        {
            if (lobby == null) return new LobbyPlayerDTO[0];

            return lobby.Players.Select(p => new LobbyPlayerDTO
            {
                UserId = p.UserId,
                Nickname = p.Nickname,
                IsReady = p.IsReady,
                IsHost = (p.UserId == lobby.HostUserId)
            }).ToArray();
        }

        public void KickPlayer(string lobbyCode, int hostUserId, string targetNickname)
        {
            if (string.IsNullOrWhiteSpace(lobbyCode) || string.IsNullOrWhiteSpace(targetNickname))
            {
                throw new ArgumentException("LobbyCode and targetNickname cannot be empty.");
            }

            try
            {
                var lobby = core.Session.GetLobby(lobbyCode);
                if (lobby == null)
                {
                    logger.LogWarning($"KickPlayer: Lobby {lobbyCode} not found.");
                    throw new InvalidOperationException("Lobby not found.");
                }

                lock (lobby.LobbyLock)
                {
                    if (lobby.HostUserId != hostUserId)
                    {
                        logger.LogWarning($"User {hostUserId} tried to kick but is not host.");
                        throw new UnauthorizedAccessException("Only the host can kick players.");
                    }

                    var targetPlayer = lobby.Players.FirstOrDefault(p =>
                        p.Nickname.Equals(targetNickname, StringComparison.OrdinalIgnoreCase));

                    if (targetPlayer == null)
                    {
                        logger.LogWarning($"KickPlayer: Player {targetNickname} not found in lobby.");
                        throw new InvalidOperationException("Player not found in lobby.");
                    }

                    if (targetPlayer.UserId == lobby.HostUserId)
                    {
                        logger.LogWarning($"Attempt to kick host {targetNickname} in lobby {lobbyCode}.");
                        throw new InvalidOperationException("Cannot kick the host.");
                    }
                }

                core.Session.Broadcast(lobbyCode, cb =>
                {
                    try
                    {
                        cb.PlayerKicked(targetNickname, "Expulsado por el anfitrión");
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning($"Failed to notify kicked player: {ex.Message}");
                    }
                });

               System.Threading.Thread.Sleep(100);

                core.Session.DisconnectPlayerCallback(lobbyCode, targetNickname);
                HandlePlayerExit(lobbyCode, targetNickname);

                logger.LogInfo($"Player {targetNickname} was kicked from lobby {lobbyCode} by host.");

                core.Session.Broadcast(lobbyCode, cb =>
                    cb.PlayerLeftLobby(targetNickname));

                core.Session.Broadcast(lobbyCode, cb =>
                    cb.UpdateListOfPlayers(MapPlayersToDTOs(lobby)));
            }
            catch (CommunicationException ex)
            {
                logger.LogWarning($"Communication error kicking player: {ex.Message}");
                throw;
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning($"Timeout kicking player: {ex.Message}");
                throw;
            }
        }
    }
}