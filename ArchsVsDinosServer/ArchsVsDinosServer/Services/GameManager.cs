using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Services.GameService;
using ArchsVsDinosServer.Services.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO.Game_DTO;
using Contracts.DTO.Game_DTO.State;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class GameManager 
    {
        /*private readonly GameSessionManager sessionManager;
        private readonly GameSetupHandler setupHandler;
        private readonly GameNotificationService notificationService;
        private readonly GameValidationService validationService;
        private readonly GameActionService actionService;
        private readonly GameQueryService queryService;
        private readonly GameExpulsionHandler expulsionHandler;
        private readonly ILoggerHelper logger;

        public GameManager()
        {
            var dependencies = new ServiceDependencies();
            sessionManager = GameSessionManager.Instance;
            setupHandler = new GameSetupHandler();
            var actionHandler = new GameActionHandler(dependencies);
            var battleResolver = new BattleResolver(dependencies);
            var endHandler = new GameEndHandler();
            logger = dependencies.loggerHelper;

            var validator = new GameRulesValidator();

            validationService = new GameValidationService(validator, logger);
            notificationService = new GameNotificationService(logger);
            expulsionHandler = new GameExpulsionHandler(sessionManager, actionHandler, notificationService, dependencies, logger);
            actionService = new GameActionService(sessionManager, actionHandler, battleResolver, endHandler, validationService, notificationService, logger);
            queryService = new GameQueryService(sessionManager, endHandler, dependencies, logger);
        }

        public GameSetupResultCode InitializeGame(int matchId)
        {

            try
            {
                lock (GameCallbackRegistry.Instance.GetMatchLock(matchId))
                {
                    var session = sessionManager.GetSession(matchId);

                    if (session == null)
                    {
                        if (!sessionManager.CreateSession(matchId))
                        {
                            return GameSetupResultCode.UnexpectedError;
                        }

                        session = sessionManager.GetSession(matchId);
                        var addPlayersResult = AddPlayersToSession(matchId, session);
                        if (addPlayersResult != GameSetupResultCode.Success)
                        {
                            return addPlayersResult;
                        }
                    }

                    var callback = OperationContext.Current.GetCallbackChannel<IGameManagerCallback>();

                    PlayerSession myPlayer = null;

                    foreach (var player in session.Players)
                    {
                        var existingCallback = GameCallbackRegistry.Instance.GetCallback(player.UserId);
                        if (existingCallback == null)
                        {
                            myPlayer = player;
                            break;
                        }
                    }

                    if (myPlayer != null)
                    {
                        myPlayer.SetCallback(callback);
                        GameCallbackRegistry.Instance.RegisterCallback(myPlayer.UserId, callback);
                        logger.LogInfo($"✅ Registered callback for userId: {myPlayer.UserId}");
                    }

                    int connectedCount = session.Players.Count(p => GameCallbackRegistry.Instance.GetCallback(p.UserId) != null);

                    if (connectedCount == session.Players.Count && !session.IsStarted)
                    {
                        ExecuteGameStart(session);
                    }

                    return GameSetupResultCode.Success;
                }
            }
            catch (ArgumentNullException ex)
            {
                logger.LogError($"Argument null to initialize game", ex);
                return GameSetupResultCode.UnexpectedError;
            }
            catch (ArgumentException ex)
            {
                logger.LogError($"Argument error while initializing game", ex);
                return GameSetupResultCode.UnexpectedError;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError($"Invalid operation while initializing game", ex);
                return GameSetupResultCode.DatabaseError;
            }
            catch (Exception ex)
            {
                logger.LogError($"Unexpected error initializing game", ex);
                return GameSetupResultCode.UnexpectedError;
            }
        }

        private GameSetupResultCode ValidateGameInitialization(int matchId)
        {
            var matchValidation = validationService.ValidateMatchId(matchId, "InitializeGame");
            if (!matchValidation.IsValid)
            {
                return GameSetupResultCode.UnexpectedError;
            }

            var existsValidation = validationService.ValidateSessionNotExists(
                sessionManager.SessionExists(matchId), matchId, "InitializeGame");
            if (!existsValidation.IsValid)
            {
                return GameSetupResultCode.GameAlreadyInitialized;
            }

            return GameSetupResultCode.Success;
        }

        private GameSession CreateGameSession(int matchId)
        {
            if (!sessionManager.CreateSession(matchId))
            {
                logger.LogInfo($"Failed to create session {matchId}");
                return null;
            }

            var session = sessionManager.GetSession(matchId);
            if (session == null)
            {
                logger.LogInfo($"Session is null after creation.");
            }

            return session;
        }

        private GameSetupResultCode AddPlayersToSession(int matchId, GameSession session)
        {
            var lobbyConfig = new BusinessLogic.MatchLobbyManagement.LobbyConfiguration();
            var lobbyPlayers = lobbyConfig.GetPlayersForGameMatch(matchId);

            if (lobbyPlayers == null || lobbyPlayers.Count < 2)
            {
                logger.LogInfo($"Not enough players for match. Found: {lobbyPlayers?.Count ?? 0}");
                sessionManager.RemoveSession(matchId);
                return GameSetupResultCode.NotEnoughPlayers;
            }

            logger.LogInfo($"Retrieved {lobbyPlayers.Count} players for match {matchId}");

            foreach (var lobbyPlayer in lobbyPlayers)
            {
                AddPlayerToSession(session, lobbyPlayer);
            }

            if (session.Players.Count < 2)
            {
                logger.LogInfo($"Failed to add enough players to session.");
                sessionManager.RemoveSession(matchId);
                return GameSetupResultCode.NotEnoughPlayers;
            }

            return GameSetupResultCode.Success;
        }

        private void AddPlayerToSession(GameSession session, Contracts.DTO.LobbyPlayerDTO lobbyPlayer)
        {
            var profileInfo = new BusinessLogic.ProfileManagement.ProfileInformation();
            var playerProfile = profileInfo.GetPlayerByUsername(lobbyPlayer.Username);

            int finalUserId;
            string finalUsername = lobbyPlayer.Username;

            if (playerProfile != null)
            {
                finalUserId = playerProfile.IdPlayer;
            }
            else
            {
                logger.LogInfo($"Profile not found for {lobbyPlayer.Username}, treating as Guest.");
                finalUserId = lobbyPlayer.IdPlayer == 0 ? lobbyPlayer.Username.GetHashCode() : lobbyPlayer.IdPlayer;
            }

            var playerSession = new PlayerSession(
                finalUserId,
                finalUsername,
                null
            );

            playerSession.TurnOrder = session.Players.Count + 1;

            session.AddPlayer(playerSession);
            logger.LogInfo($"Added player to session: {finalUsername} (ID: {finalUserId})");
        }

        public GameSetupResultCode StartGame(int matchId)
        {
            try
            {
                var session = sessionManager.GetSession(matchId);
                var sessionValidation = validationService.ValidateSessionExists(session, matchId, "StartGame");
                if (!sessionValidation.IsValid)
                {
                    return GameSetupResultCode.MatchNotFound;
                }

                if (session.IsStarted)
                {
                    return GameSetupResultCode.GameAlreadyInitialized;
                }

                var playerCountValidation = validationService.ValidatePlayerCount(session.Players.Count, "StartGame");
                if (!playerCountValidation.IsValid)
                {
                    return GameSetupResultCode.NotEnoughPlayers;
                }

                return ExecuteGameStart(session);
            }
            catch (ArgumentException ex)
            {
                logger.LogError($"Invalid argument to start game", ex);
                return GameSetupResultCode.UnexpectedError;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError($"Invalid operation to start game", ex);
                return GameSetupResultCode.DatabaseError;
            }
            catch (Exception ex)
            {
                logger.LogError($"Unexpected error while stranting game", ex);
                return GameSetupResultCode.UnexpectedError;
            }
        }

        private GameSetupResultCode ExecuteGameStart(GameSession session)
        {
            var players = session.Players.ToList();
            if (!setupHandler.InitializeGameSession(session, players))
            {
                logger.LogInfo($"Failed to initialize game session");
                return GameSetupResultCode.UnexpectedError;
            }

            var firstPlayer = setupHandler.SelectFirstPlayer(session);
            if (firstPlayer == null)
            {
                logger.LogInfo($"Failed to select first player for session");
                return GameSetupResultCode.UnexpectedError;
            }

            session.StartTurn(firstPlayer.UserId);
            session.MarkAsStarted();
            notificationService.NotifyGameStarted(session, firstPlayer, new GameEndHandler());

            logger.LogInfo($"Session started successfully");
            return GameSetupResultCode.Success;
        }

        public DrawCardResultCode DrawCard(int matchId, int userId, int drawPileNumber)
        {
            return actionService.DrawCard(matchId, userId, drawPileNumber);
        }

        public PlayCardResultCode PlayDinoHead(int matchId, int userId, int cardId)
        {
            return actionService.PlayDinoHead(matchId, userId, cardId);
        }

        public PlayCardResultCode AttachBodyPartToDino(int matchId, int userId, int cardId, int dinoHeadCardId)
        {
            return actionService.AttachBodyPartToDino(matchId, userId, cardId, dinoHeadCardId);
        }

        public ProvokeResultCode ProvokeArchArmy(int matchId, int userId, string armyType)
        {
            return actionService.ProvokeArchArmy(matchId, userId, armyType);
        }

        public EndTurnResultCode EndTurn(int matchId, int userId)
        {
            return actionService.EndTurn(matchId, userId);
        }

        public GameStateDTO GetGameState(int matchId)
        {
            return queryService.GetGameState(matchId);
        }

        public PlayerHandDTO GetPlayerHand(int matchId, int userId)
        {
            return queryService.GetPlayerHand(matchId, userId);
        }

        public CentralBoardDTO GetCentralBoard(int matchId)
        {
            return queryService.GetCentralBoard(matchId);
        }

        public void NotifyPlayerExpelled(string matchCode, string username, string reason)
        {
            expulsionHandler.NotifyPlayerExpelled(matchCode, username, reason);
        }

        public void NotifyGameClosure(string matchCode, string reason)
        {
            expulsionHandler.NotifyGameClosure(matchCode, reason);
        }*/

    }
}
