using ArchsVsDinosServer.BusinessLogic.Game_Management;
using ArchsVsDinosServer.BusinessLogic.Game_Manager.Cards;
using ArchsVsDinosServer.BusinessLogic.Game_Manager.Session;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO.Game_DTO;
using Contracts.DTO.Game_DTO.State;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class GameManager : IGameManager
    {
        private readonly GameSessionManager sessionManager;
        private readonly GameSetupHandler setupHandler;
        private readonly GameActionHandler actionHandler;
        private readonly BattleResolver battleResolver;
        private readonly GameRulesValidator validator;
        private readonly GameEndHandler endHandler;
        private readonly CardHelper cardHelper;
        private readonly ServiceDependencies dependencies;
        private readonly ILoggerHelper logger;

        public GameManager()
        {
            dependencies = new ServiceDependencies();
            sessionManager = GameSessionManager.Instance;
            setupHandler = new GameSetupHandler(dependencies);
            actionHandler = new GameActionHandler(dependencies);
            battleResolver = new BattleResolver(dependencies);
            validator = new GameRulesValidator();
            endHandler = new GameEndHandler();
            cardHelper = new CardHelper(dependencies);
            logger = dependencies.loggerHelper;
        }

        public GameSetupResultCode InitializeGame(int matchId)
        {
            try
            {
                if (matchId <= 0)
                {
                    logger.LogInfo($"InitializeGame: Invalid matchId {matchId}");
                    return GameSetupResultCode.UnexpectedError;
                }

                if (sessionManager.SessionExists(matchId))
                {
                    logger.LogInfo($"InitializeGame: Session {matchId} already exists");
                    return GameSetupResultCode.GameAlreadyInitialized;
                }

                if (!sessionManager.CreateSession(matchId))
                {
                    logger.LogInfo($"InitializeGame: Failed to create session {matchId}");
                    return GameSetupResultCode.UnexpectedError;
                }

                var session = sessionManager.GetSession(matchId);
                NotifyGameInitialized(session);

                logger.LogInfo($"InitializeGame: Session {matchId} initialized successfully");
                return GameSetupResultCode.Success;
            }
            catch (ArgumentNullException ex)
            {
                logger.LogError($"InitializeGame: Argument null - {ex.Message}", ex);
                return GameSetupResultCode.UnexpectedError;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError($"InitializeGame: Invalid operation - {ex.Message}", ex);
                return GameSetupResultCode.UnexpectedError;
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

                if (session == null)
                {
                    logger.LogInfo($"StartGame: Session {matchId} not found");
                    return GameSetupResultCode.MatchNotFound;
                }

                if (session.IsStarted)
                {
                    logger.LogInfo($"StartGame: Session {matchId} already started");
                    return GameSetupResultCode.GameAlreadyInitialized;
                }

                if (session.Players.Count < 2 || session.Players.Count > 4)
                {
                    logger.LogInfo($"StartGame: Invalid player count {session.Players.Count} for session {matchId}");
                    return GameSetupResultCode.NotEnoughPlayers;
                }

                var players = session.Players.ToList();
                if (!setupHandler.InitializeGameSession(session, players))
                {
                    logger.LogInfo($"StartGame: Failed to initialize game session {matchId}");
                    return GameSetupResultCode.UnexpectedError;
                }

                var firstPlayer = setupHandler.SelectFirstPlayer(session);
                if (firstPlayer == null)
                {
                    logger.LogInfo($"StartGame: Failed to select first player for session {matchId}");
                    return GameSetupResultCode.UnexpectedError;
                }

                session.StartTurn(firstPlayer.UserId);
                session.MarkAsStarted();

                NotifyGameStarted(session, firstPlayer);

                logger.LogInfo($"StartGame: Session {matchId} started successfully");
                return GameSetupResultCode.Success;
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

        public DrawCardResultCode DrawCard(int matchId, int userId, int drawPileNumber)
        {
            try
            {
                var session = sessionManager.GetSession(matchId);

                if (session == null)
                {
                    logger.LogInfo($"DrawCard: Session {matchId} not found");
                    return DrawCardResultCode.UnexpectedError;
                }

                if (!session.IsStarted)
                {
                    logger.LogInfo($"DrawCard: Game {matchId} not started");
                    return DrawCardResultCode.GameNotStarted;
                }

                var player = sessionManager.GetPlayer(matchId, userId);

                if (player == null)
                {
                    logger.LogInfo($"DrawCard: Player {userId} not found in session {matchId}");
                    return DrawCardResultCode.UnexpectedError;
                }

                if (session.CurrentTurn != userId)
                {
                    logger.LogInfo($"DrawCard: Not player {userId} turn");
                    return DrawCardResultCode.NotYourTurn;
                }

                if (session.HasDrawnThisTurn)
                {
                    logger.LogInfo($"DrawCard: Player {userId} already drew this turn");
                    return DrawCardResultCode.AlreadyDrewThisTurn;
                }

                if (!validator.CanDrawCard(session, userId))
                {
                    logger.LogInfo($"DrawCard: Player {userId} cannot draw card now");
                    return DrawCardResultCode.AlreadyDrewThisTurn;
                }

                if (drawPileNumber < 0 || drawPileNumber >= session.DrawPiles.Count)
                {
                    logger.LogWarning($"DrawCard: Invalid pile number {drawPileNumber}");
                    return DrawCardResultCode.InvalidDrawPile;
                }

                if (session.GetDrawPileCount(drawPileNumber) == 0)
                {
                    logger.LogInfo($"DrawCard: Pile {drawPileNumber} is empty");
                    return DrawCardResultCode.DrawPileEmpty;
                }

                var card = actionHandler.DrawCard(session, player, drawPileNumber);
                if (card == null)
                {
                    logger.LogInfo($"DrawCard: Failed to draw card for player {userId}");
                    return DrawCardResultCode.UnexpectedError;
                }

                NotifyCardDrawn(session, player, card, drawPileNumber);
                CheckAndHandleGameEnd(session);

                logger.LogInfo($"DrawCard: Player {userId} drew card from pile {drawPileNumber}");
                return DrawCardResultCode.Success;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                logger.LogError($"DrawCard: Index out of range - {ex.Message}", ex);
                return DrawCardResultCode.InvalidDrawPile;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError($"DrawCard: Invalid operation - {ex.Message}", ex);
                return DrawCardResultCode.DatabaseError;
            }
            catch (Exception ex)
            {
                logger.LogInfo($"DrawCard: Unexpected error - {ex.Message}");
                return DrawCardResultCode.UnexpectedError;
            }
        }

        public PlayCardResultCode PlayDinoHead(int matchId, int userId, int cardId)
        {
            try
            {
                var session = sessionManager.GetSession(matchId);

                if (session == null)
                {
                    logger.LogInfo($"PlayDinoHead: Session {matchId} not found");
                    return PlayCardResultCode.UnexpectedError;
                }

                var player = sessionManager.GetPlayer(matchId, userId);

                if (player == null)
                {
                    logger.LogInfo($"PlayDinoHead: Player {userId} not found");
                    return PlayCardResultCode.UnexpectedError;
                }

                if (session.CurrentTurn != userId)
                {
                    logger.LogInfo($"PlayDinoHead: Not player {userId} turn");
                    return PlayCardResultCode.NotYourTurn;
                }

                if (session.CardsPlayedThisTurn >= 2)
                {
                    logger.LogInfo($"PlayDinoHead: Player {userId} already played 2 cards");
                    return PlayCardResultCode.AlreadyPlayedTwoCards;
                }

                if (!validator.CanPlayCard(session, userId))
                {
                    logger.LogInfo($"PlayDinoHead: Player {userId} cannot play card now");
                    return PlayCardResultCode.AlreadyPlayedTwoCards;
                }

                var card = player.Hand.FirstOrDefault(c => c.IdCardBody == cardId || c.IdCardCharacter == cardId);

                if (card == null)
                {
                    logger.LogInfo($"PlayDinoHead: Card {cardId} not in player {userId} hand");
                    return PlayCardResultCode.CardNotInHand;
                }

                if (!validator.IsValidDinoHead(card))
                {
                    logger.LogInfo($"PlayDinoHead: Card {cardId} is not a valid dino head");
                    return PlayCardResultCode.InvalidDinoHead;
                }

                var dino = actionHandler.PlayDinoHead(session, player, card.IdCardGlobal);
                if (dino == null)
                {
                    logger.LogInfo($"PlayDinoHead: Failed to play dino head {cardId}");
                    return PlayCardResultCode.InvalidCardType;
                }

                NotifyDinoPlayed(session, player, dino);

                logger.LogInfo($"PlayDinoHead: Player {userId} played dino head {cardId}");
                return PlayCardResultCode.Success;
            }
            catch (ArgumentNullException ex)
            {
                logger.LogError($"PlayDinoHead: Null argument - {ex.Message}", ex);
                return PlayCardResultCode.UnexpectedError;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError($"PlayDinoHead: Invalid operation - {ex.Message}", ex);
                return PlayCardResultCode.DatabaseError;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"PlayDinoHead: Unexpected error - {ex.Message}");
                return PlayCardResultCode.UnexpectedError;
            }
        }

        public PlayCardResultCode AttachBodyPartToDino(int matchId, int userId, int cardId, int dinoHeadCardId)
        {
            try
            {
                var session = sessionManager.GetSession(matchId);

                if (session == null)
                {
                    logger.LogInfo($"AttachBodyPart: Session {matchId} not found");
                    return PlayCardResultCode.UnexpectedError;
                }

                var player = sessionManager.GetPlayer(matchId, userId);

                if (player == null)
                {
                    logger.LogInfo($"AttachBodyPart: Player {userId} not found");
                    return PlayCardResultCode.UnexpectedError;
                }

                if (session.CurrentTurn != userId)
                {
                    logger.LogInfo($"AttachBodyPart: Not player {userId} turn");
                    return PlayCardResultCode.NotYourTurn;
                }

                if (session.CardsPlayedThisTurn >= 2)
                {
                    logger.LogInfo($"AttachBodyPart: Player {userId} already played 2 cards");
                    return PlayCardResultCode.AlreadyPlayedTwoCards;
                }

                if (!validator.CanPlayCard(session, userId))
                {
                    logger.LogInfo($"AttachBodyPart: Player {userId} cannot play card now");
                    return PlayCardResultCode.AlreadyPlayedTwoCards;
                }

                var bodyCard = player.Hand.FirstOrDefault(c => c.IdCardBody == cardId || c.IdCardCharacter == cardId);
                var dino = player.Dinos.FirstOrDefault(d =>
                    d.HeadCard.IdCardBody == dinoHeadCardId ||
                    d.HeadCard.IdCardCharacter == dinoHeadCardId);

                if (bodyCard == null)
                {
                    logger.LogInfo($"AttachBodyPart: Body card {cardId} not in hand");
                    return PlayCardResultCode.CardNotInHand;
                }

                if (dino == null)
                {
                    logger.LogInfo($"AttachBodyPart: Dino with head {dinoHeadCardId} not found");
                    return PlayCardResultCode.MustAttachToHead;
                }

                if (!validator.IsValidBodyPart(bodyCard))
                {
                    logger.LogInfo($"AttachBodyPart: Card {cardId} is not a valid body part");
                    return PlayCardResultCode.InvalidCardType;
                }

                if (bodyCard.ArmyType != dino.ArmyType)
                {
                    logger.LogInfo($"AttachBodyPart: Army type mismatch");
                    return PlayCardResultCode.ArmyTypeMismatch;
                }

                var success = actionHandler.AttachBodyPart(session, player, bodyCard.IdCardGlobal, dino.HeadCard.IdCardGlobal);
                if (!success)
                {
                    logger.LogInfo($"AttachBodyPart: Cannot attach body {cardId} to dino {dinoHeadCardId}");
                    return PlayCardResultCode.ArmyTypeMismatch;
                }

                NotifyBodyPartAttached(session, player, dino, bodyCard);

                logger.LogInfo($"AttachBodyPart: Player {userId} attached body {cardId} to dino {dinoHeadCardId}");
                return PlayCardResultCode.Success;
            }
            catch (ArgumentNullException ex)
            {
                logger.LogError($"AttachBodyPart: Null argument - {ex.Message}", ex);
                return PlayCardResultCode.UnexpectedError;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError($"AttachBodyPart: Invalid operation - {ex.Message}", ex);
                return PlayCardResultCode.DatabaseError;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"AttachBodyPart: Unexpected error - {ex.Message}");
                return PlayCardResultCode.UnexpectedError;
            }
        }

        public ProvokeResultCode ProvokeArchArmy(int matchId, int userId, string armyType)
        {
            try
            {
                var session = sessionManager.GetSession(matchId);

                if (session == null)
                {
                    logger.LogInfo($"ProvokeArmy: Session {matchId} not found");
                    return ProvokeResultCode.UnexpectedError;
                }

                var player = sessionManager.GetPlayer(matchId, userId);

                if (player == null)
                {
                    logger.LogInfo($"ProvokeArmy: Player {userId} not found");
                    return ProvokeResultCode.UnexpectedError;
                }

                if (session.CurrentTurn != userId)
                {
                    logger.LogInfo($"ProvokeArmy: Not player {userId} turn");
                    return ProvokeResultCode.NotYourTurn;
                }

                if (!validator.CanProvoke(session, userId))
                {
                    logger.LogInfo($"ProvokeArmy: Player {userId} cannot provoke now");
                    return ProvokeResultCode.AlreadyTookAction;
                }

                if (!ArmyTypeHelper.IsValidBaseType(armyType))
                {
                    logger.LogInfo($"ProvokeArmy: Invalid army type {armyType}");
                    return ProvokeResultCode.InvalidArmyType;
                }

                var army = session.CentralBoard.GetArmyByType(armyType);
                if (army == null || army.Count == 0)
                {
                    logger.LogInfo($"ProvokeArmy: Army {armyType} is empty");
                    return ProvokeResultCode.NoArchsInArmy;
                }

                session.MarkMainActionTaken();

                var battleResult = battleResolver.ResolveBattle(session, armyType);
                if (battleResult == null)
                {
                    logger.LogInfo($"ProvokeArmy: Failed to resolve battle for {armyType}");
                    return ProvokeResultCode.UnexpectedError;
                }

                NotifyBattleResolved(session, player, battleResult);

                logger.LogInfo($"ProvokeArmy: Player {userId} provoked {armyType} army");
                return ProvokeResultCode.Success;
            }
            catch (ArgumentException ex)
            {
                logger.LogError($"ProvokeArmy: Invalid argument - {ex.Message}", ex);
                return ProvokeResultCode.InvalidArmyType;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError($"ProvokeArmy: Invalid operation - {ex.Message}", ex);
                return ProvokeResultCode.DatabaseError;
            }
            catch (Exception ex)
            {
                logger.LogInfo($"ProvokeArmy: Unexpected error - {ex.Message}");
                return ProvokeResultCode.UnexpectedError;
            }
        }

        public EndTurnResultCode EndTurn(int matchId, int userId)
        {
            try
            {
                var session = sessionManager.GetSession(matchId);

                if (session == null)
                {
                    logger.LogInfo($"EndTurn: Session {matchId} not found");
                    return EndTurnResultCode.UnexpectedError;
                }

                if (session.CurrentTurn != userId)
                {
                    logger.LogInfo($"EndTurn: Not player {userId} turn");
                    return EndTurnResultCode.NotYourTurn;
                }

                if (!validator.CanEndTurn(session, userId))
                {
                    logger.LogInfo($"EndTurn: Player {userId} cannot end turn");
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
                NotifyTurnChanged(session, nextPlayer);

                logger.LogInfo($"EndTurn: Turn changed to player {nextPlayer.UserId}");
                return EndTurnResultCode.Success;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError($"EndTurn: Invalid operation - {ex.Message}", ex);
                return EndTurnResultCode.DatabaseError;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"EndTurn: Unexpected error - {ex.Message}");
                return EndTurnResultCode.UnexpectedError;
            }
        }

        public GameStateDTO GetGameState(int matchId)
        {
            try
            {
                var session = sessionManager.GetSession(matchId);
                if (session == null)
                {
                    logger.LogWarning($"GetGameState: Session {matchId} not found");
                    return null;
                }

                return BuildGameStateDTO(session);
            }
            catch (CommunicationException ex)
            {
                logger.LogWarning("GetGameState: Communication issue");
                return null;
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning("GetGameState: Timeout");
                return null;
            }
            catch (EntityException ex)
            {
                logger.LogError("GetGameState: Database/Entity error", ex);
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError($"GetGameState: Unexpected error - {ex.Message}", ex);
                return null;
            }
        }

        public PlayerHandDTO GetPlayerHand(int matchId, int userId)
        {
            try
            {
                var player = sessionManager.GetPlayer(matchId, userId);
                if (player == null)
                {
                    logger.LogWarning($"GetPlayerHand: Player {userId} not found in session {matchId}");
                    return null;
                }

                return new PlayerHandDTO
                {
                    UserId = userId,
                    Cards = player.Hand.Select(c => CardHelper.ConvertToCardDTO(c)).ToList()
                };
            }
            catch (CommunicationException ex)
            {
                logger.LogWarning("GetPlayerHand: Communication issue");
                return null;
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning("GetPlayerHand: Timeout");
                return null;
            }
            catch (EntityException ex)
            {
                logger.LogError("GetPlayerHand: Database/Entity error", ex);
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError($"GetPlayerHand: Unexpected error - {ex.Message}", ex);
                return null;
            }
        }

        public CentralBoardDTO GetCentralBoard(int matchId)
        {
            try
            {
                var session = sessionManager.GetSession(matchId);
                if (session == null)
                {
                    logger.LogWarning($"GetCentralBoard: Session {matchId} not found");
                    return null;
                }

                return BuildCentralBoardDTO(session);
            }
            catch (CommunicationException ex)
            {
                logger.LogWarning("GetCentralBoard: Communication issue");
                return null;
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning("GetCentralBoard: Timeout");
                return null;
            }
            catch (EntityException ex)
            {
                logger.LogError("GetCentralBoard: Database/Entity error", ex);
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError($"GetCentralBoard: Unexpected error - {ex.Message}", ex);
                return null;
            }
        }

        private bool CheckAndHandleGameEnd(GameSession session)
        {
            if (endHandler.ShouldGameEnd(session))
            {
                var result = endHandler.EndGame(session);
                if (result != null)
                {
                    NotifyGameEnded(session, result);
                    sessionManager.RemoveSession(session.MatchId);
                    return true;
                }
            }
            return false;
        }

        private GameStateDTO BuildGameStateDTO(GameSession session)
        {
            return new GameStateDTO
            {
                MatchId = session.MatchId,
                IsStarted = session.IsStarted,
                CurrentTurnUserId = session.CurrentTurn,
                TurnNumber = session.TurnNumber,
                RemainingTime = endHandler.GetRemainingTime(session),
                Players = session.Players.Select(p => new PlayerInGameDTO
                {
                    UserId = p.UserId,
                    Username = p.Username,
                    TurnOrder = p.TurnOrder
                }).ToList(),
                CentralBoard = BuildCentralBoardDTO(session),
                DrawPile1Count = session.GetDrawPileCount(0),
                DrawPile2Count = session.GetDrawPileCount(1),
                DrawPile3Count = session.GetDrawPileCount(2),
                DiscardPileCount = session.DiscardPile.Count
            };
        }

        private CentralBoardDTO BuildCentralBoardDTO(GameSession session)
        {
            return new CentralBoardDTO
            {
                LandArmyCount = session.CentralBoard.LandArmy.Count,
                SeaArmyCount = session.CentralBoard.SeaArmy.Count,
                SkyArmyCount = session.CentralBoard.SkyArmy.Count,
                LandArmyPower = session.CentralBoard.GetArmyPower("land", cardHelper),
                SeaArmyPower = session.CentralBoard.GetArmyPower("sea", cardHelper),
                SkyArmyPower = session.CentralBoard.GetArmyPower("sky", cardHelper)
            };
        }

        private void NotifyGameInitialized(GameSession session)
        {
            var dto = new GameInitializedDTO
            {
                MatchId = session.MatchId,
                Players = session.Players.Select(p => new PlayerInGameDTO
                {
                    UserId = p.UserId,
                    Username = p.Username,
                    TurnOrder = p.TurnOrder
                }).ToList()
            };

            NotifyAllPlayers(session, p => p.Callback?.OnGameInitialized(dto));
        }

        private void NotifyGameStarted(GameSession session, PlayerSession firstPlayer)
        {
            var dto = new GameStartedDTO
            {
                MatchId = session.MatchId,
                FirstPlayerUserId = firstPlayer.UserId,
                FirstPlayerUsername = firstPlayer.Username,
                PlayersHands = session.Players.Select(p => new PlayerHandDTO
                {
                    UserId = p.UserId,
                    Cards = p.Hand.Select(c => CardHelper.ConvertToCardDTO(c)).ToList()
                }).ToList(),
                DrawPile1Count = session.GetDrawPileCount(0),
                DrawPile2Count = session.GetDrawPileCount(1),
                DrawPile3Count = session.GetDrawPileCount(2),
                StartTime = session.StartTime ?? DateTime.UtcNow
            };

            NotifyAllPlayers(session, p => p.Callback?.OnGameStarted(dto));
        }

        private void NotifyTurnChanged(GameSession session, PlayerSession currentPlayer)
        {
            var dto = new TurnChangedDTO
            {
                MatchId = session.MatchId,
                CurrentPlayerUserId = currentPlayer.UserId,
                CurrentPlayerUsername = currentPlayer.Username,
                TurnNumber = session.TurnNumber,
                RemainingTime = endHandler.GetRemainingTime(session)
            };

            NotifyAllPlayers(session, p => p.Callback?.OnTurnChanged(dto));
        }

        private void NotifyCardDrawn(GameSession session, PlayerSession player, CardInGame card, int pileNumber)
        {
            var dto = new CardDrawnDTO
            {
                MatchId = session.MatchId,
                PlayerUserId = player.UserId,
                PlayerUsername = player.Username,
                DrawPileNumber = pileNumber,
                Card = CardHelper.ConvertToCardDTO(card)
            };

            NotifyAllPlayers(session, p => p.Callback?.OnCardDrawn(dto));
        }

        private void NotifyDinoPlayed(GameSession session, PlayerSession player, DinoInstance dino)
        {
            var dto = new DinoPlayedDTO
            {
                MatchId = session.MatchId,
                PlayerUserId = player.UserId,
                PlayerUsername = player.Username,
                DinoInstanceId = dino.DinoInstanceId,
                HeadCard = CardHelper.ConvertToCardDTO(dino.HeadCard),
                ArmyType = dino.ArmyType
            };

            NotifyAllPlayers(session, p => p.Callback?.OnDinoHeadPlayed(dto));
        }

        private void NotifyBodyPartAttached(GameSession session, PlayerSession player, DinoInstance dino, CardInGame bodyCard)
        {
            var dto = new BodyPartAttachedDTO
            {
                MatchId = session.MatchId,
                PlayerUserId = player.UserId,
                PlayerUsername = player.Username,
                DinoInstanceId = dino.DinoInstanceId,
                BodyCard = CardHelper.ConvertToCardDTO(bodyCard),
                NewTotalPower = dino.GetTotalPower()
            };

            NotifyAllPlayers(session, p => p.Callback?.OnBodyPartAttached(dto));
        }

        private void NotifyBattleResolved(GameSession session, PlayerSession provoker, BattleResult battleResult)
        {
            // Crear DTO de resultado de batalla
            var dto = new BattleResultDTO
            {
                MatchId = session.MatchId,
                ArmyType = battleResult.ArmyType,
                ArchPower = battleResult.ArchPower,
                DinosWon = battleResult.DinosWon,
                WinnerUserId = battleResult.Winner?.UserId,
                WinnerUsername = battleResult.Winner?.Username,
                WinnerPower = battleResult.WinnerPower,
                PointsAwarded = battleResult.DinosWon ? battleResult.ArchPower : 0,
                ArchCards = battleResult.ArchCardIds
                    .Select(id => CardHelper.ConvertToCardDTO(cardHelper.CreateCardInGame(id)))
                    .Where(c => c != null)
                    .ToList(),
                PlayerPowers = new Dictionary<int, int>()
            };

            foreach (var kvp in battleResult.PlayerDinos)
            {
                var totalPower = kvp.Value.Sum(d => d.GetTotalPower());
                dto.PlayerPowers[kvp.Key] = totalPower;
            }

            var provokeDto = new ArchArmyProvokedDTO
            {
                MatchId = session.MatchId,
                ProvokerUserId = provoker.UserId,
                ProvokerUsername = provoker.Username,
                ArmyType = battleResult.ArmyType,
                BattleResult = dto
            };

            NotifyAllPlayers(session, p => p.Callback?.OnArchArmyProvoked(provokeDto));
            NotifyAllPlayers(session, p => p.Callback?.OnBattleResolved(dto));
        }

        private void NotifyGameEnded(GameSession session, GameEndResult result)
        {
            var finalScores = session.Players
                .OrderByDescending(p => p.Points)
                .Select((p, index) => new PlayerScoreDTO
                {
                    UserId = p.UserId,
                    Username = p.Username,
                    Points = p.Points,
                    Position = index + 1
                }).ToList();

            var dto = new GameEndedDTO
            {
                MatchId = session.MatchId,
                Reason = result.Reason,
                WinnerUserId = result.Winner?.UserId ?? 0,
                WinnerUsername = result.Winner?.Username ?? string.Empty,
                WinnerPoints = result.WinnerPoints,
                FinalScores = finalScores
            };

            NotifyAllPlayers(session, p => p.Callback?.OnGameEnded(dto));
        }

        private void NotifyAllPlayers(GameSession session, Action<PlayerSession> notifyAction)
        {
            foreach (var player in session.Players)
            {
                try
                {
                    notifyAction(player);
                }
                catch (CommunicationException ex)
                {
                    logger.LogWarning($"NotifyAllPlayers: Communication issue notifying player {player.UserId}");
                }
                catch (TimeoutException ex)
                {
                    logger.LogWarning($"NotifyAllPlayers: Timeout notifying player {player.UserId}");
                }
                catch (Exception ex)
                {
                    logger.LogInfo($"Failed to notify player {player.UserId}: {ex.Message}");
                }
            }
        }
    }
}
