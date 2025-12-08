using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces;
using Contracts.DTO.Result_Codes;
using System;

namespace ArchsVsDinosServer.Services.GameService
{
    public class GameActionService
    {
        private readonly GameSessionManager sessionManager;
        private readonly GameActionHandler actionHandler;
        private readonly BattleResolver battleResolver;
        private readonly GameEndHandler endHandler;
        private readonly GameValidationService validationService;
        private readonly GameNotificationService notificationService;
        private readonly ILoggerHelper logger;

        public GameActionService(
            GameSessionManager sessionManager,
            GameActionHandler actionHandler,
            BattleResolver battleResolver,
            GameEndHandler endHandler,
            GameValidationService validationService,
            GameNotificationService notificationService,
            ILoggerHelper logger)
        {
            this.sessionManager = sessionManager;
            this.actionHandler = actionHandler;
            this.battleResolver = battleResolver;
            this.endHandler = endHandler;
            this.validationService = validationService;
            this.notificationService = notificationService;
            this.logger = logger;
        }

        public DrawCardResultCode DrawCard(int matchId, int userId, int drawPileNumber)
        {
            try
            {
                var session = sessionManager.GetSession(matchId);
                var sessionValidation = validationService.ValidateSessionExists(session, matchId, "DrawCard");
                if (!sessionValidation.IsValid)
                {
                    return DrawCardResultCode.UnexpectedError;
                }

                var gameStartedValidation = validationService.ValidateGameStarted(session, "DrawCard");
                if (!gameStartedValidation.IsValid)
                {
                    return DrawCardResultCode.GameNotStarted;
                }

                var player = sessionManager.GetPlayer(matchId, userId);
                var playerValidation = validationService.ValidatePlayerExists(player, userId, matchId, "DrawCard");
                if (!playerValidation.IsValid)
                {
                    return DrawCardResultCode.UnexpectedError;
                }

                var turnValidation = validationService.ValidatePlayerTurn(session, userId, "DrawCard");
                if (!turnValidation.IsValid)
                {
                    return DrawCardResultCode.NotYourTurn;
                }

                var canDrawValidation = validationService.ValidateCanDrawCard(session, userId, "DrawCard");
                if (!canDrawValidation.IsValid)
                {
                    return DrawCardResultCode.AlreadyDrewThisTurn;
                }

                var pileValidation = validationService.ValidateDrawPile(session, drawPileNumber, "DrawCard");
                if (!pileValidation.IsValid)
                {
                    return pileValidation.ErrorMessage == "Invalid draw pile"
                        ? DrawCardResultCode.InvalidDrawPile
                        : DrawCardResultCode.DrawPileEmpty;
                }

                return ExecuteDrawCard(session, player, drawPileNumber);
            }
            catch (Exception ex)
            {
                return HandleDrawCardError(ex);
            }
        }

        private DrawCardResultCode ExecuteDrawCard(GameSession session, PlayerSession player, int drawPileNumber)
        {

            if (!session.ConsumeMove()) return DrawCardResultCode.AlreadyDrewThisTurn;
            var card = actionHandler.DrawCard(session, player, drawPileNumber);
            if (card == null)
            {
                logger.LogInfo($"DrawCard: Failed to draw card for player {player.UserId}");
                return DrawCardResultCode.UnexpectedError;
            }

            notificationService.NotifyCardDrawn(session, player, card, drawPileNumber);
            CheckAndHandleGameEnd(session);

            logger.LogInfo($"DrawCard: Player {player.UserId} drew card from pile {drawPileNumber}");

            if (session.RemainingMoves == 0)
            {
                EndTurn(session.MatchId, player.UserId);
            }
            return DrawCardResultCode.Success;
        }

        public PlayCardResultCode PlayDinoHead(int matchId, int userId, int cardId)
        {
            try
            {
                var session = sessionManager.GetSession(matchId);
                var sessionValidation = validationService.ValidateSessionExists(session, matchId, "PlayDinoHead");
                if (!sessionValidation.IsValid)
                {
                    return PlayCardResultCode.UnexpectedError;
                }

                var player = sessionManager.GetPlayer(matchId, userId);
                var playerValidation = validationService.ValidatePlayerExists(player, userId, matchId, "PlayDinoHead");
                if (!playerValidation.IsValid)
                {
                    return PlayCardResultCode.UnexpectedError;
                }

                var turnValidation = validationService.ValidatePlayerTurn(session, userId, "PlayDinoHead");
                if (!turnValidation.IsValid)
                {
                    return PlayCardResultCode.NotYourTurn;
                }

                var canPlayValidation = validationService.ValidateCanPlayCard(session, userId, "PlayDinoHead");
                if (!canPlayValidation.IsValid)
                {
                    return PlayCardResultCode.AlreadyPlayedTwoCards;
                }

                return ExecutePlayDinoHead(session, player, cardId);
            }
            catch (Exception ex)
            {
                return HandlePlayCardError("PlayDinoHead", ex);
            }
        }

        private PlayCardResultCode ExecutePlayDinoHead(GameSession session, PlayerSession player, int cardId)
        {
            var cardValidation = validationService.ValidateCardInHand(player, cardId, "PlayDinoHead");
            if (!cardValidation.IsValid)
            {
                return PlayCardResultCode.CardNotInHand;
            }

            var dinoHeadValidation = validationService.ValidateDinoHead(cardValidation.Data, cardId, "PlayDinoHead");
            if (!dinoHeadValidation.IsValid)
            {
                return PlayCardResultCode.InvalidDinoHead;
            }

            if (!session.ConsumeMove()) return PlayCardResultCode.AlreadyPlayedTwoCards;

            var dino = actionHandler.PlayDinoHead(session, player, cardId);
            if (dino == null)
            {
                logger.LogInfo($"PlayDinoHead: Failed to play dino head {cardId}");
                return PlayCardResultCode.UnexpectedError;
            }

            notificationService.NotifyDinoPlayed(session, player, dino);

            logger.LogInfo($"PlayDinoHead: Player {player.UserId} played dino head {cardId}");

            if (session.RemainingMoves == 0)
            {
                EndTurn(session.MatchId, player.UserId);
            }
            return PlayCardResultCode.Success;
        }

        public PlayCardResultCode AttachBodyPartToDino(int matchId, int userId, int cardId, int dinoHeadCardId)
        {
            try
            {
                var session = sessionManager.GetSession(matchId);
                var sessionValidation = validationService.ValidateSessionExists(session, matchId, "AttachBodyPart");
                if (!sessionValidation.IsValid)
                {
                    return PlayCardResultCode.UnexpectedError;
                }

                var player = sessionManager.GetPlayer(matchId, userId);
                var playerValidation = validationService.ValidatePlayerExists(player, userId, matchId, "AttachBodyPart");
                if (!playerValidation.IsValid)
                {
                    return PlayCardResultCode.UnexpectedError;
                }

                var turnValidation = validationService.ValidatePlayerTurn(session, userId, "AttachBodyPart");
                if (!turnValidation.IsValid)
                {
                    return PlayCardResultCode.NotYourTurn;
                }

                var canPlayValidation = validationService.ValidateCanPlayCard(session, userId, "AttachBodyPart");
                if (!canPlayValidation.IsValid)
                {
                    return PlayCardResultCode.AlreadyPlayedTwoCards;
                }

                return ExecuteAttachBodyPart(session, player, cardId, dinoHeadCardId);
            }
            catch (Exception ex)
            {
                return HandlePlayCardError("AttachBodyPart", ex);
            }
        }

        private PlayCardResultCode ExecuteAttachBodyPart(GameSession session, PlayerSession player, int cardId, int dinoHeadCardId)
        {
            var cardValidation = validationService.ValidateCardInHand(player, cardId, "AttachBodyPart");
            if (!cardValidation.IsValid)
            {
                return PlayCardResultCode.CardNotInHand;
            }

            var dinoValidation = validationService.ValidateDinoExists(player, dinoHeadCardId, "AttachBodyPart");
            if (!dinoValidation.IsValid)
            {
                return PlayCardResultCode.MustAttachToHead;
            }

            var bodyPartValidation = validationService.ValidateBodyPart(cardValidation.Data, cardId, "AttachBodyPart");
            if (!bodyPartValidation.IsValid)
            {
                return PlayCardResultCode.InvalidCardType;
            }

            var armyTypeValidation = validationService.ValidateArmyTypeMatch(cardValidation.Data, dinoValidation.Data, "AttachBodyPart");
            if (!armyTypeValidation.IsValid)
            {
                return PlayCardResultCode.ArmyTypeMismatch;
            }

            if (!session.ConsumeMove()) return PlayCardResultCode.AlreadyPlayedTwoCards;

            var success = actionHandler.AttachBodyPart(session, player, cardId, dinoHeadCardId);
            if (!success)
            {
                logger.LogInfo($"AttachBodyPart: Cannot attach body {cardId} to dino {dinoHeadCardId}");
                return PlayCardResultCode.ArmyTypeMismatch;
            }

            notificationService.NotifyBodyPartAttached(session, player, dinoValidation.Data, cardValidation.Data);
            logger.LogInfo($"AttachBodyPart: Player {player.UserId} attached body {cardId} to dino {dinoHeadCardId}");
            return PlayCardResultCode.Success;
        }

        public ProvokeResultCode ProvokeArchArmy(int matchId, int userId, string armyType)
        {
            try
            {
                var session = sessionManager.GetSession(matchId);
                var sessionValidation = validationService.ValidateSessionExists(session, matchId, "ProvokeArmy");
                if (!sessionValidation.IsValid)
                {
                    return ProvokeResultCode.UnexpectedError;
                }

                var player = sessionManager.GetPlayer(matchId, userId);
                var playerValidation = validationService.ValidatePlayerExists(player, userId, matchId, "ProvokeArmy");
                if (!playerValidation.IsValid)
                {
                    return ProvokeResultCode.UnexpectedError;
                }

                var turnValidation = validationService.ValidatePlayerTurn(session, userId, "ProvokeArmy");
                if (!turnValidation.IsValid)
                {
                    return ProvokeResultCode.NotYourTurn;
                }

                var canProvokeValidation = validationService.ValidateCanProvoke(session, userId, "ProvokeArmy");
                if (!canProvokeValidation.IsValid)
                {
                    return ProvokeResultCode.AlreadyTookAction;
                }

                var armyTypeValidation = validationService.ValidateArmyType(armyType, "ProvokeArmy");
                if (!armyTypeValidation.IsValid)
                {
                    return ProvokeResultCode.InvalidArmyType;
                }

                var armyNotEmptyValidation = validationService.ValidateArmyNotEmpty(session, armyType, "ProvokeArmy");
                if (!armyNotEmptyValidation.IsValid)
                {
                    return ProvokeResultCode.NoArchsInArmy;
                }

                return ExecuteProvokeArmy(session, player, armyType);
            }
            catch (Exception ex)
            {
                return HandleProvokeError(ex);
            }
        }

        private ProvokeResultCode ExecuteProvokeArmy(GameSession session, PlayerSession player, string armyType)
        {
            session.MarkMainActionTaken();

            var battleResult = battleResolver.ResolveBattle(session, armyType);
            if (battleResult == null)
            {
                logger.LogInfo($"ProvokeArmy: Failed to resolve battle for {armyType}");
                return ProvokeResultCode.UnexpectedError;
            }

            notificationService.NotifyBattleResolved(session, player, battleResult);
            logger.LogInfo($"ProvokeArmy: Player {player.UserId} provoked {armyType} army");
            return ProvokeResultCode.Success;
        }

        public EndTurnResultCode EndTurn(int matchId, int userId)
        {
            try
            {
                var session = sessionManager.GetSession(matchId);
                var sessionValidation = validationService.ValidateSessionExists(session, matchId, "EndTurn");
                if (!sessionValidation.IsValid)
                {
                    return EndTurnResultCode.UnexpectedError;
                }

                var turnValidation = validationService.ValidatePlayerTurn(session, userId, "EndTurn");
                if (!turnValidation.IsValid)
                {
                    return EndTurnResultCode.NotYourTurn;
                }

                var canEndTurnValidation = validationService.ValidateCanEndTurn(session, userId, "EndTurn");
                if (!canEndTurnValidation.IsValid)
                {
                    return EndTurnResultCode.NotYourTurn;
                }

                if (CheckAndHandleGameEnd(session))
                {
                    logger.LogInfo($"EndTurn: Game {matchId} ended");
                    return EndTurnResultCode.GameEnded;
                }

                var nextPlayer = actionHandler.GetNextPlayer(session);
                if (nextPlayer == null)
                {
                    logger.LogInfo($"EndTurn: Failed to get next player");
                    return EndTurnResultCode.UnexpectedError;
                }

                session.StartTurn(nextPlayer.UserId);
                notificationService.NotifyTurnChanged(session, nextPlayer, endHandler);

                logger.LogInfo($"EndTurn: Turn changed to player {nextPlayer.UserId}");
                return EndTurnResultCode.Success;
            }
            catch (Exception ex)
            {
                return HandleEndTurnError(ex);
            }
        }

        private bool CheckAndHandleGameEnd(GameSession session)
        {
            if (endHandler.ShouldGameEnd(session))
            {
                var result = endHandler.EndGame(session);
                if (result != null)
                {
                    notificationService.NotifyGameEnded(session, result);
                    sessionManager.RemoveSession(session.MatchId);
                    return true;
                }
            }
            return false;
        }

        private DrawCardResultCode HandleDrawCardError(Exception ex)
        {
            if (ex is ArgumentOutOfRangeException)
            {
                logger.LogError($"DrawCard: Index out of range", ex);
                return DrawCardResultCode.InvalidDrawPile;
            }

            if (ex is InvalidOperationException)
            {
                logger.LogError($"DrawCard: Invalid operation", ex);
                return DrawCardResultCode.DatabaseError;
            }

            logger.LogInfo($"DrawCard: Unexpected error");
            return DrawCardResultCode.UnexpectedError;
        }

        private PlayCardResultCode HandlePlayCardError(string operation, Exception ex)
        {
            if (ex is ArgumentNullException)
            {
                logger.LogError($"{operation}: Null argument", ex);
                return PlayCardResultCode.UnexpectedError;
            }

            if (ex is InvalidOperationException)
            {
                logger.LogError($"{operation}: Invalid operation", ex);
                return PlayCardResultCode.DatabaseError;
            }

            logger.LogWarning($"{operation}: Unexpected error");
            return PlayCardResultCode.UnexpectedError;
        }

        private ProvokeResultCode HandleProvokeError(Exception ex)
        {
            if (ex is ArgumentException)
            {
                logger.LogError($"ProvokeArmy: Invalid argument", ex);
                return ProvokeResultCode.InvalidArmyType;
            }

            if (ex is InvalidOperationException)
            {
                logger.LogError($"ProvokeArmy: Invalid operation", ex);
                return ProvokeResultCode.DatabaseError;
            }

            logger.LogInfo($"ProvokeArmy: Unexpected error");
            return ProvokeResultCode.UnexpectedError;
        }

        private EndTurnResultCode HandleEndTurnError(Exception ex)
        {
            if (ex is InvalidOperationException)
            {
                logger.LogError($"EndTurn: Invalid operation", ex);
                return EndTurnResultCode.DatabaseError;
            }

            logger.LogWarning($"EndTurn: Unexpected error");
            return EndTurnResultCode.UnexpectedError;
        }
    }
}
