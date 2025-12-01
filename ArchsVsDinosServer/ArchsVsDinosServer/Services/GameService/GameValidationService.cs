using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces;
using System.Linq;

namespace ArchsVsDinosServer.Services.GameService
{
    public class GameValidationService
    {
        private readonly GameRulesValidator rulesValidator;
        private readonly ILoggerHelper logger;

        public GameValidationService(GameRulesValidator rulesValidator, ILoggerHelper logger)
        {
            this.rulesValidator = rulesValidator;
            this.logger = logger;
        }

        #region Session Validation

        public ValidationResult<GameSession> ValidateSessionExists(GameSession session, int matchId, string operation)
        {
            if (session == null)
            {
                logger.LogInfo($"{operation}: Session {matchId} not found");
                return ValidationResult<GameSession>.Fail("Session not found");
            }

            return ValidationResult<GameSession>.Success(session);
        }

        public ValidationResult<GameSession> ValidateGameStarted(GameSession session, string operation)
        {
            if (!session.IsStarted)
            {
                logger.LogInfo($"{operation}: Game {session.MatchId} not started");
                return ValidationResult<GameSession>.Fail("Game not started");
            }

            return ValidationResult<GameSession>.Success(session);
        }

        #endregion

        #region Player Validation

        public ValidationResult<PlayerSession> ValidatePlayerExists(PlayerSession player, int userId, int matchId, string operation)
        {
            if (player == null)
            {
                logger.LogInfo($"{operation}: Player {userId} not found in session {matchId}");
                return ValidationResult<PlayerSession>.Fail("Player not found");
            }

            return ValidationResult<PlayerSession>.Success(player);
        }

        public ValidationResult ValidatePlayerTurn(GameSession session, int userId, string operation)
        {
            if (session.CurrentTurn != userId)
            {
                logger.LogInfo($"{operation}: Not player {userId} turn");
                return ValidationResult.Fail("Not your turn");
            }

            return ValidationResult.Success();
        }

        #endregion

        #region Draw Card Validation

        public ValidationResult ValidateCanDrawCard(GameSession session, int userId, string operation)
        {
            if (session.HasDrawnThisTurn)
            {
                logger.LogInfo($"{operation}: Player {userId} already drew this turn");
                return ValidationResult.Fail("Already drew this turn");
            }

            if (!rulesValidator.CanDrawCard(session, userId))
            {
                logger.LogInfo($"{operation}: Player {userId} cannot draw card now");
                return ValidationResult.Fail("Cannot draw card now");
            }

            return ValidationResult.Success();
        }

        public ValidationResult ValidateDrawPile(GameSession session, int drawPileNumber, string operation)
        {
            if (drawPileNumber < 0 || drawPileNumber >= session.DrawPiles.Count)
            {
                logger.LogWarning($"{operation}: Invalid pile number {drawPileNumber}");
                return ValidationResult.Fail("Invalid draw pile");
            }

            if (session.GetDrawPileCount(drawPileNumber) == 0)
            {
                logger.LogInfo($"{operation}: Pile {drawPileNumber} is empty");
                return ValidationResult.Fail("Draw pile empty");
            }

            return ValidationResult.Success();
        }

        #endregion

        #region Play Card Validation

        public ValidationResult ValidateCanPlayCard(GameSession session, int userId, string operation)
        {
            if (session.CardsPlayedThisTurn >= 2)
            {
                logger.LogInfo($"{operation}: Player {userId} already played 2 cards");
                return ValidationResult.Fail("Already played two cards");
            }

            if (!rulesValidator.CanPlayCard(session, userId))
            {
                logger.LogInfo($"{operation}: Player {userId} cannot play card now");
                return ValidationResult.Fail("Cannot play card now");
            }

            return ValidationResult.Success();
        }

        public ValidationResult<CardInGame> ValidateCardInHand(PlayerSession player, int cardId, string operation)
        {
            var card = player.Hand.FirstOrDefault(c => c.IdCard == cardId);

            if (card == null)
            {
                logger.LogInfo($"{operation}: Card {cardId} not in player {player.UserId} hand");
                return ValidationResult<CardInGame>.Fail("Card not in hand");
            }

            return ValidationResult<CardInGame>.Success(card);
        }

        public ValidationResult ValidateDinoHead(CardInGame card, int cardId, string operation)
        {
            if (!rulesValidator.IsValidDinoHead(card))
            {
                logger.LogInfo($"{operation}: Card {cardId} is not a valid dino head");
                return ValidationResult.Fail("Invalid dino head");
            }

            return ValidationResult.Success();
        }

        #endregion

        #region Attach Body Part Validation

        public ValidationResult<DinoInstance> ValidateDinoExists(PlayerSession player, int dinoHeadCardId, string operation)
        {
            var dino = player.Dinos.FirstOrDefault(d => d.HeadCard.IdCard == dinoHeadCardId);

            if (dino == null)
            {
                logger.LogInfo($"{operation}: Dino with head {dinoHeadCardId} not found");
                return ValidationResult<DinoInstance>.Fail("Dino not found");
            }

            return ValidationResult<DinoInstance>.Success(dino);
        }

        public ValidationResult ValidateBodyPart(CardInGame bodyCard, int cardId, string operation)
        {
            if (!rulesValidator.IsValidBodyPart(bodyCard))
            {
                logger.LogInfo($"{operation}: Card {cardId} is not a valid body part");
                return ValidationResult.Fail("Invalid body part");
            }

            return ValidationResult.Success();
        }

        public ValidationResult ValidateArmyTypeMatch(CardInGame bodyCard, DinoInstance dino, string operation)
        {
            var normalizedBodyElement = ArmyTypeHelper.NormalizeElement(bodyCard.Element);
            var normalizedDinoElement = ArmyTypeHelper.NormalizeElement(dino.Element);

            if (normalizedBodyElement != normalizedDinoElement)
            {
                logger.LogInfo($"{operation}: Element mismatch - Body: {bodyCard.Element}, Dino: {dino.Element}");
                return ValidationResult.Fail("Element mismatch");
            }

            return ValidationResult.Success();
        }

        #endregion

        #region Provoke Army Validation

        public ValidationResult ValidateCanProvoke(GameSession session, int userId, string operation)
        {
            if (!rulesValidator.CanProvoke(session, userId))
            {
                logger.LogInfo($"{operation}: Player {userId} cannot provoke now");
                return ValidationResult.Fail("Already took action");
            }

            return ValidationResult.Success();
        }

        public ValidationResult ValidateArmyType(string armyType, string operation)
        {
            if (!ArmyTypeHelper.IsValidBaseType(armyType))
            {
                logger.LogInfo($"{operation}: Invalid army type {armyType}");
                return ValidationResult.Fail("Invalid army type");
            }

            return ValidationResult.Success();
        }

        public ValidationResult ValidateArmyNotEmpty(GameSession session, string armyType, string operation)
        {
            var normalizedArmyType = ArmyTypeHelper.NormalizeElement(armyType);
            var army = session.CentralBoard.GetArmyByType(normalizedArmyType);

            if (army == null || army.Count == 0)
            {
                logger.LogInfo($"{operation}: Army {armyType} is empty");
                return ValidationResult.Fail("No archs in army");
            }

            return ValidationResult.Success();
        }

        #endregion

        #region End Turn Validation

        public ValidationResult ValidateCanEndTurn(GameSession session, int userId, string operation)
        {
            if (!rulesValidator.CanEndTurn(session, userId))
            {
                logger.LogInfo($"{operation}: Player {userId} cannot end turn");
                return ValidationResult.Fail("Cannot end turn");
            }

            return ValidationResult.Success();
        }

        #endregion

        #region Initialize Game Validation

        public ValidationResult ValidateMatchId(int matchId, string operation)
        {
            if (matchId <= 0)
            {
                logger.LogInfo($"{operation}: Invalid matchId {matchId}");
                return ValidationResult.Fail("Invalid match ID");
            }

            return ValidationResult.Success();
        }

        public ValidationResult ValidateSessionNotExists(bool sessionExists, int matchId, string operation)
        {
            if (sessionExists)
            {
                logger.LogInfo($"{operation}: Session {matchId} already exists");
                return ValidationResult.Fail("Game already initialized");
            }

            return ValidationResult.Success();
        }

        public ValidationResult ValidatePlayerCount(int playerCount, string operation)
        {
            if (playerCount < 2 || playerCount > 4)
            {
                logger.LogInfo($"{operation}: Invalid player count {playerCount}");
                return ValidationResult.Fail("Invalid player count");
            }

            return ValidationResult.Success();
        }

        #endregion
    }

    #region Validation Result Classes

    public class ValidationResult
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; }

        private ValidationResult(bool isValid, string errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }

        public static ValidationResult Success()
        {
            return new ValidationResult(true);
        }

        public static ValidationResult Fail(string errorMessage)
        {
            return new ValidationResult(false, errorMessage);
        }
    }

    public class ValidationResult<T>
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; }
        public T Data { get; private set; }

        private ValidationResult(bool isValid, T data = default(T), string errorMessage = null)
        {
            IsValid = isValid;
            Data = data;
            ErrorMessage = errorMessage;
        }

        public static ValidationResult<T> Success(T data)
        {
            return new ValidationResult<T>(true, data);
        }

        public static ValidationResult<T> Fail(string errorMessage)
        {
            return new ValidationResult<T>(false, default(T), errorMessage);
        }
    }

    #endregion
}