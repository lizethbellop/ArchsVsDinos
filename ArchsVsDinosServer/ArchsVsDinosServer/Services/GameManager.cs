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
    public class GameManager : IGameManager, IGameNotifier
    {
        private readonly GameSessionManager sessionManager;
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

                if (!sessionManager.CreateSession(matchId))
                {
                    logger.LogInfo($"InitializeGame: Failed to create session {matchId}");
                    return GameSetupResultCode.UnexpectedError;
                }

                var session = sessionManager.GetSession(matchId);
                notificationService.NotifyGameInitialized(session);

                logger.LogInfo($"InitializeGame: Session {matchId} initialized successfully");
                return GameSetupResultCode.Success;
            }
            catch (ArgumentNullException ex)
            {
                logger.LogError($"InitializeGame: Argument null - {ex.Message}", ex);
                return GameSetupResultCode.UnexpectedError;
            }
            catch (ArgumentException ex)
            {
                logger.LogError($"InitializeGame: Argument error - {ex.Message}", ex);
                return GameSetupResultCode.UnexpectedError;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError($"InitializeGame: Invalid operation - {ex.Message}", ex);
                return GameSetupResultCode.DatabaseError;
            }
            catch (Exception ex)
            {
                logger.LogInfo($"InitializeGame: Unexpected error - {ex.Message}");
                return GameSetupResultCode.UnexpectedError;
            }
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
                    logger.LogInfo($"StartGame: Session {matchId} already started");
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
                logger.LogError($"StartGame: Invalid argument - {ex.Message}", ex);
                return GameSetupResultCode.UnexpectedError;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError($"StartGame: Invalid operation - {ex.Message}", ex);
                return GameSetupResultCode.DatabaseError;
            }
            catch (Exception ex)
            {
                logger.LogInfo($"StartGame: Unexpected error - {ex.Message}");
                return GameSetupResultCode.UnexpectedError;
            }
        }

        private GameSetupResultCode ExecuteGameStart(GameSession session)
        {
            var players = session.Players.ToList();
            if (!setupHandler.InitializeGameSession(session, players))
            {
                logger.LogInfo($"StartGame: Failed to initialize game session {session.MatchId}");
                return GameSetupResultCode.UnexpectedError;
            }

            var firstPlayer = setupHandler.SelectFirstPlayer(session);
            if (firstPlayer == null)
            {
                logger.LogInfo($"StartGame: Failed to select first player for session {session.MatchId}");
                return GameSetupResultCode.UnexpectedError;
            }

            session.StartTurn(firstPlayer.UserId);
            session.MarkAsStarted();
            notificationService.NotifyGameStarted(session, firstPlayer, new GameEndHandler());

            logger.LogInfo($"StartGame: Session {session.MatchId} started successfully");
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
        }

    }
}
