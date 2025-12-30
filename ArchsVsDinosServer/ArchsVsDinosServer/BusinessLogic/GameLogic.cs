using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Model;
using ArchsVsDinosServer.Services;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Game_DTO;
using Contracts.DTO.Game_DTO.Enums;
using Contracts.DTO.Game_DTO.State;
using Contracts.DTO.Result_Codes;
using Contracts.DTO.Statistics;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic
{
    public class GameLogic : IGameLogic
    {
        private readonly ILoggerHelper loggerHelper;
        private readonly GameCoreContext gameCoreContext;
        private readonly GameRulesValidator rulesValidator;
        private readonly GameEndHandler gameEndHandler;
        private readonly IGameNotifier gameNotifier;
        private readonly IStatisticsManager statisticsManager;

        public GameLogic(GameCoreContext coreContext, ILoggerHelper logger, IGameNotifier notifier, IStatisticsManager statsManager)
        {
            this.gameCoreContext = coreContext ?? throw new ArgumentNullException(nameof(coreContext));
            this.loggerHelper = logger ?? throw new ArgumentNullException(nameof(logger));
            this.rulesValidator = new GameRulesValidator();
            this.gameEndHandler = new GameEndHandler();
            this.gameNotifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
            this.statisticsManager = statsManager ?? throw new ArgumentNullException(nameof(statsManager));
        }

        public bool AttachBodyPart(string matchCode, int userId, AttachBodyPartDTO attachmentData)
        {
            if (attachmentData == null) throw new ArgumentNullException(nameof(attachmentData));

            var gameSession = GetActiveSession(matchCode);

            lock (gameSession.SyncRoot)
            {
                var playerSession = GetPlayer(gameSession, userId);
                var dinoInstance = rulesValidator.FindDinoByHeadCardId(playerSession, attachmentData.DinoHeadCardId);

                var cardToAttach = playerSession.GetCardById(attachmentData.CardId);

                if (cardToAttach == null)
                    throw new InvalidOperationException($"Card {attachmentData.CardId} not found in player's hand.");

                if (!rulesValidator.IsValidBodyPart(cardToAttach))
                    throw new InvalidOperationException($"Card {attachmentData.CardId} is not a valid body part.");

                if (!rulesValidator.CanAttachBodyPart(cardToAttach, dinoInstance))
                    throw new InvalidOperationException($"Cannot attach card {cardToAttach.IdCard} to Dino {dinoInstance.DinoInstanceId}.");

                if (!dinoInstance.TryAddBodyPart(cardToAttach) || !playerSession.RemoveCard(cardToAttach))
                    throw new InvalidOperationException($"Could not attach or remove card {cardToAttach.IdCard}.");

                loggerHelper.LogInfo($"Player {userId} attached card {cardToAttach.IdCard} to Dino {dinoInstance.DinoInstanceId} in {matchCode}.");

                var context = new AttachBodyPartContext
                {
                    Session = gameSession,
                    Player = playerSession,
                    Dino = dinoInstance,
                    Card = cardToAttach
                };

                var dto = CreateBodyPartAttachedDTO(context);
                gameNotifier.NotifyBodyPartAttached(dto);
                return true;
            }
        }

        public CardInGame DrawCard(string matchCode, int userId)
        {
            var gameSession = GetActiveSession(matchCode);

            if (gameSession.CurrentTurn != userId)
                throw new InvalidOperationException("It is not the player's turn.");

            var playerSession = GetPlayer(gameSession, userId);

            lock (gameSession.SyncRoot)
            {
                if (!gameSession.ConsumeMoves(1))
                    throw new InvalidOperationException("No moves remaining.");

                var drawnCardIds = gameSession.DrawCards(1);

                if (drawnCardIds == null || drawnCardIds.Count == 0)
                    throw new InvalidOperationException("The Draw Deck is empty.");

                var drawnCard = CardInGame.FromDefinition(drawnCardIds[0]);

                if (drawnCard == null)
                    throw new InvalidOperationException($"Invalid card drawn: ID {drawnCardIds[0]}");

                if (drawnCard.IsArch())
                {
                    gameSession.CentralBoard.AddArchCardToArmy(drawnCard);
                    var archAddedDto = new ArchAddedToBoardDTO
                    {
                        MatchId = matchCode.GetHashCode(),
                        PlayerUserId = userId,
                        PlayerUsername = playerSession.Nickname,
                        ArchCard = CreateCardDTO(drawnCard),
                        ArmyType = drawnCard.Element.ToString(),
                        NewArchCount = gameSession.CentralBoard.GetArmyByType(drawnCard.Element).Count
                    };
                    gameNotifier.NotifyArchAddedToBoard(archAddedDto);
                }
                else
                {
                    playerSession.AddCard(drawnCard);
                }

                loggerHelper.LogInfo($"Player {userId} drew card {drawnCard.IdCard} from DrawDeck in match {matchCode}");

                var dto = CreateCardDrawnDTO(gameSession, playerSession, drawnCard);
                gameNotifier.NotifyCardDrawn(dto);
                return drawnCard;
            }
        }

        public bool EndTurn(string matchCode, int userId)
        {
            var gameSession = GetActiveSession(matchCode);
            var currentPlayer = gameSession.Players.FirstOrDefault(player => player.UserId == userId);

            if (currentPlayer == null) throw new InvalidOperationException("Player not found.");

            lock (gameSession.SyncRoot)
            {
                var nextPlayer = gameSession.Players.OrderBy(player => player.TurnOrder)
                                                    .SkipWhile(player => player.UserId != userId)
                                                    .Skip(1)
                                                    .DefaultIfEmpty(gameSession.Players.OrderBy(player => player.TurnOrder).First())
                                                    .First();

                gameSession.EndTurn(nextPlayer.UserId);
                loggerHelper.LogInfo($"Turn ended for {userId}. Next: {nextPlayer.UserId} in {matchCode}");

                gameNotifier.NotifyTurnChanged(new TurnChangedDTO
                {
                    MatchCode = matchCode,
                    CurrentPlayerUserId = nextPlayer.UserId,
                    TurnNumber = gameSession.TurnNumber,
                    RemainingTime = TimeSpan.Zero,
                    PlayerScores = gameSession.Players.ToDictionary(player => player.UserId, player => player.Points)
                });

                return true;
            }
        }

        public bool ExchangeCard(string matchCode, int userId, ExchangeCardDTO exchangeData)
        {
            var gameSession = GetActiveSession(matchCode);
            var playerA = GetPlayer(gameSession, userId);
            var playerB = GetPlayer(gameSession, exchangeData.TargetUserId);

            if (playerA == null || playerB == null)
                return false;

            lock (gameSession.SyncRoot)
            {
                if (!gameSession.ConsumeMoves(1))
                    return false;

                var cardFromA = playerA.GetCardById(exchangeData.OfferedCardId);
                var cardFromB = playerB.GetCardById(exchangeData.RequestedCardId);

                if (cardFromA == null || cardFromB == null)
                    return false;

                if (cardFromA.PartType != cardFromB.PartType)
                    return false;

                var exchangeContext = new CardExchangeContext
                {
                    MatchCode = matchCode,
                    PlayerA = playerA,
                    PlayerB = playerB,
                    CardFromA = cardFromA,
                    CardFromB = cardFromB
                };

                ExecuteCardExchange(exchangeContext);
                return true;
            }
        }

        private void ExecuteCardExchange(CardExchangeContext context)
        {
            context.PlayerA.RemoveCard(context.CardFromA);
            context.PlayerB.RemoveCard(context.CardFromB);

            context.PlayerA.AddCard(context.CardFromB);
            context.PlayerB.AddCard(context.CardFromA);

            NotifyCardExchange(context);
        }

        public Task<bool> InitializeMatch(string matchCode, List<GamePlayerInitDTO> initialPlayers)
        {
            if (!IsValidInitialization(matchCode, initialPlayers))
                return Task.FromResult(false);

            var gameSession = CreateGameSession(matchCode);
            if (gameSession == null)
                return Task.FromResult(false);

            var playerSessions = CreatePlayerSessions(initialPlayers);

            if (!SetupGame(gameSession, playerSessions))
                return Task.FromResult(false);

            StartFirstTurn(gameSession);
            gameSession.MarkAsStarted();

            loggerHelper.LogInfo($"InitializeMatch: Match {matchCode} initialized successfully.");

            NotifyGameStarted(gameSession);

            var initDto = new GameInitializedDTO
            {
                MatchCode = matchCode,
                Players = playerSessions.Select((player, index) => new PlayerInGameDTO
                {
                    UserId = player.UserId,
                    TurnOrder = index + 1
                }).ToList(),
                RemainingCardsInDeck = gameSession.DrawDeck.Count
            };

            gameNotifier.NotifyGameInitialized(initDto);

            return Task.FromResult(true);
        }

        private void NotifyGameStarted(GameSession session)
        {
            var failedPlayers = new List<int>();

            foreach (var player in session.Players)
            {
                var playerHandDto = new PlayerHandDTO
                {
                    UserId = player.UserId,
                    Cards = player.Hand
                        .Where(card => !card.IsArch()) 
                        .Select(card => CreateCardDTO(card))
                        .Where(dto => dto != null)
                        .ToList()
                };

                var gameStartedDto = new GameStartedDTO
                {
                    MatchId = session.MatchCode.GetHashCode(),
                    FirstPlayerUserId = session.CurrentTurn,
                    FirstPlayerUsername = session.Players.FirstOrDefault(playerSelected => playerSelected.UserId == session.CurrentTurn)?.Nickname ?? string.Empty,
                    MyUserId = player.UserId,
                    StartTime = session.StartTime ?? DateTime.UtcNow,
                    PlayersHands = new List<PlayerHandDTO> { playerHandDto },
                    InitialBoard = MapBoardToDTO(session.CentralBoard),
                    DrawDeckCount = session.DrawDeck.Count
                };

                try
                {
                    gameNotifier.NotifyGameStarted(gameStartedDto);
                }
                catch (Exception ex)
                {
                    loggerHelper.LogError($"Failed to notify game start to player {player.UserId} ({player.Nickname})", ex);
                    failedPlayers.Add(player.UserId);
                }
            }

            if (failedPlayers.Count > 0)
            {
                loggerHelper.LogWarning($"Game start notification failed for {failedPlayers.Count} players in {session.MatchCode}. Aborting game.");

                foreach (var userId in failedPlayers)
                {
                    session.RemovePlayer(userId);
                }

                if (session.Players.Count < 2)
                {
                    EndGame(session.MatchCode, GameEndType.Aborted, "Insufficient players after failed connections");
                }
            }
            else
            {
                loggerHelper.LogInfo($"Game started successfully for all {session.Players.Count} players in {session.MatchCode}");
            }

            NotifyInitialArchsOnBoard(session);
        }

        private CentralBoardDTO MapBoardToDTO(CentralBoard board)
        {
            return new CentralBoardDTO
            {
                SandArmyCount = board.SandArmy.Count,
                WaterArmyCount = board.WaterArmy.Count,
                WindArmyCount = board.WindArmy.Count,

                SandArmy = board.SandArmy
                    .Select(id => CardInGame.FromDefinition(id))
                    .Where(c => c != null)
                    .Select(c => CreateCardDTO(c))
                    .ToList(),

                WaterArmy = board.WaterArmy
                    .Select(id => CardInGame.FromDefinition(id))
                    .Where(c => c != null)
                    .Select(c => CreateCardDTO(c))
                    .ToList(),

                WindArmy = board.WindArmy
                    .Select(id => CardInGame.FromDefinition(id))
                    .Where(c => c != null)
                    .Select(c => CreateCardDTO(c))
                    .ToList()
            };
        }

        public DinoInstance PlayDinoHead(string matchCode, int userId, int headCardId)
        {
            var gameSession = GetActiveSession(matchCode);
            var playerSession = GetPlayer(gameSession, userId);

            var headCard = playerSession.GetCardById(headCardId);
            if (headCard == null) throw new InvalidOperationException("Card not found in player's hand.");
            if (!rulesValidator.IsValidDinoHead(headCard)) throw new InvalidOperationException("Card is not a valid dino head.");

            lock (gameSession.SyncRoot)
            {
                var newDino = new DinoInstance(playerSession.GetNextDinoId(), headCard);
                if (!playerSession.RemoveCard(headCard)) throw new InvalidOperationException("Could not remove card.");
                playerSession.AddDino(newDino);

                var dto = CreateDinoHeadPlayedDTO(gameSession, playerSession, newDino);
                gameNotifier.NotifyDinoHeadPlayed(dto);

                return newDino;
            }
        }

        public Task<bool> Provoke(string matchCode, int userId, ArmyType targetArmy)
        {
            var gameSession = GetActiveSession(matchCode);
            var playerSession = GetPlayer(gameSession, userId);

            if (!rulesValidator.CanProvoke(gameSession, userId))
                throw new InvalidOperationException("Cannot provoke, insufficient moves.");

            lock (gameSession.SyncRoot)
            {
                gameSession.ConsumeMoves(gameSession.RemainingMoves);

                var battleResolver = new BattleResolver(new ServiceDependencies());
                var battleResult = battleResolver.ResolveBattle(gameSession, targetArmy);

                var battleResultDto = CreateBattleResultDTO(matchCode, battleResult);

                var provokedDto = new ArchArmyProvokedDTO
                {
                    MatchCode = matchCode,
                    ProvokerUserId = userId,
                    ArmyType = targetArmy,
                    BattleResult = battleResultDto
                };

                gameNotifier.NotifyArchArmyProvoked(provokedDto);

                DiscardArmy(gameSession, targetArmy);
                DiscardDinos(gameSession);

                loggerHelper.LogInfo($"Player {userId} provoked {targetArmy} in {matchCode}. DinosWon: {battleResult?.DinosWon}, Winner: {battleResult?.Winner?.Nickname ?? "Nobody"}");

                var nextPlayer = gameSession.Players.OrderBy(player => player.TurnOrder)
                                                    .SkipWhile(player => player.UserId != userId)
                                                    .Skip(1)
                                                    .DefaultIfEmpty(gameSession.Players.OrderBy(player => player.TurnOrder).First())
                                                    .First();

                gameSession.EndTurn(nextPlayer.UserId);
                loggerHelper.LogInfo($"[PROVOKE] Turn auto-ended. Next player: {nextPlayer.UserId}");

                gameNotifier.NotifyTurnChanged(new TurnChangedDTO
                {
                    MatchCode = matchCode,
                    CurrentPlayerUserId = nextPlayer.UserId,
                    TurnNumber = gameSession.TurnNumber,
                    RemainingTime = TimeSpan.Zero,
                    PlayerScores = gameSession.Players.ToDictionary(player => player.UserId, player => player.Points)
                });

                return Task.FromResult(true);
            }
        }

        private bool IsValidInitialization(string matchCode, List<GamePlayerInitDTO> playersList)
        {
            if (string.IsNullOrWhiteSpace(matchCode) || playersList == null || playersList.Count < 2 || playersList.Count > 4)
            {
                loggerHelper.LogWarning("InitializeMatch: Invalid parameters.");
                return false;
            }

            if (gameCoreContext.Sessions.SessionExists(matchCode))
            {
                loggerHelper.LogWarning($"InitializeMatch: Session {matchCode} already exists.");
                return false;
            }

            return true;
        }

        private GameSession CreateGameSession(string matchCode)
        {
            if (!gameCoreContext.Sessions.CreateSession(matchCode))
            {
                loggerHelper.LogWarning($"CreateGameSession: Failed to create session {matchCode}");
                return null;
            }

            var session = gameCoreContext.Sessions.GetSession(matchCode);
            if (session == null)
            {
                loggerHelper.LogWarning($"CreateGameSession: Failed to retrieve session {matchCode}");
                gameCoreContext.Sessions.RemoveSession(matchCode);
            }

            return session;
        }

        private List<PlayerSession> CreatePlayerSessions(List<GamePlayerInitDTO> initialPlayers)
        {
            return initialPlayers
                .Select(playerDto => new PlayerSession(playerDto.UserId, playerDto.Nickname, callback: null))
                .ToList();
        }

        private bool SetupGame(GameSession session, List<PlayerSession> playerSessions)
        {
            if (!gameCoreContext.Setup.InitializeGameSession(session, playerSessions))
            {
                loggerHelper.LogWarning($"SetupGame: Setup failed for match {session.MatchCode}");
                gameCoreContext.Sessions.RemoveSession(session.MatchCode);
                return false;
            }

            return true;
        }

        private void StartFirstTurn(GameSession session)
        {
            var firstPlayer = gameCoreContext.Setup.SelectFirstPlayer(session);
            if (firstPlayer != null)
            {
                session.StartTurn(firstPlayer.UserId);
            }
        }

        private GameSession GetActiveSession(string matchCode)
        {
            if (string.IsNullOrWhiteSpace(matchCode))
                throw new ArgumentException("matchCode cannot be null or empty.", nameof(matchCode));

            var session = gameCoreContext.Sessions.GetSession(matchCode);
            if (session == null)
                throw new InvalidOperationException($"Session {matchCode} not found.");

            if (!session.IsStarted || session.IsFinished)
                throw new InvalidOperationException("The game is not active.");

            return session;
        }

        private PlayerSession GetPlayer(GameSession session, int userId)
        {
            var player = session.Players.FirstOrDefault(p => p.UserId == userId);
            if (player == null)
                throw new InvalidOperationException($"Player {userId} not found in session {session.MatchCode}.");

            return player;
        }

        private void DiscardDinos(GameSession session)
        {
            foreach (var playerSession in session.Players)
            {
                var dinoCards = playerSession.Dinos.SelectMany(dino => dino.GetAllCards()).Select(card => card.IdCard).ToList();
                playerSession.ClearDinos();
                session.AddToDiscard(dinoCards);
            }
        }

        private void DiscardArmy(GameSession session, ArmyType armyType)
        {
            var discardedIds = session.CentralBoard.ClearArmy(armyType);

            session.AddToDiscard(discardedIds);
        }

        public GameEndResult EndGame(string matchCode, GameEndType gameType, string reason)
        {
            if (string.IsNullOrWhiteSpace(matchCode))
                throw new ArgumentException(nameof(matchCode));

            var session = gameCoreContext.Sessions.GetSession(matchCode)
                ?? throw new InvalidOperationException($"Session {matchCode} not found.");

            lock (session.SyncRoot)
            {
                GameEndResult result;

                if (gameType == GameEndType.Finished)
                {
                    if (!gameEndHandler.ShouldGameEnd(session))
                        throw new InvalidOperationException("The game cannot end yet.");

                    result = gameEndHandler.EndGame(session)
                        ?? throw new InvalidOperationException("Could not calculate the result.");

                    TrySaveMatchStatistics(session, result);
                }
                else
                {
                    result = new GameEndResult
                    {
                        Winner = null,
                        WinnerPoints = 0,
                        Reason = reason
                    };
                }

                session.MarkAsFinished(gameType);
                gameCoreContext.Sessions.RemoveSession(matchCode);

                gameNotifier.NotifyGameEnded(new GameEndedDTO
                {
                    MatchCode = matchCode,
                    WinnerUserId = result.Winner?.UserId ?? 0,
                    Reason = reason,
                    WinnerPoints = result.WinnerPoints
                });

                return result;
            }
        }

        private void SaveMatchStatistics(GameSession session, GameEndResult result)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session), "Session cannot be null.");
            if (result == null)
                throw new ArgumentNullException(nameof(result), "GameEndResult cannot be null.");

            if (statisticsManager == null)
                throw new InvalidOperationException("StatisticsManager is not initialized.");

            var matchResultDto = new MatchResultDTO
            {
                MatchId = session.MatchCode,
                MatchDate = session.StartTime ?? DateTime.UtcNow,

                PlayerResults = session.Players
                .Where(player => player.UserId > 0)
                .Select(player =>
                {
                    int archaeologistsEliminated = player.Dinos.Sum(dino => dino.ArchaeologistsEliminated);
                    int supremeBossesEliminated = player.Dinos.Sum(dino => dino.SupremeBossesEliminated);

                    return new PlayerMatchResultDTO
                    {
                        UserId = player.UserId,
                        Points = player.Points,
                        IsWinner = result.Winner?.UserId == player.UserId,
                        ArchaeologistsEliminated = archaeologistsEliminated,
                        SupremeBossesEliminated = supremeBossesEliminated
                    };
                }).ToList()
            };

            if (matchResultDto.PlayerResults.Count == 0)
            {
                loggerHelper.LogInfo($"No registered players in match {session.MatchCode}, skipping statistics save");
                return;
            }

            var saveCode = statisticsManager.SaveMatchStatistics(matchResultDto);
            if (saveCode != SaveMatchResultCode.Success)
                throw new InvalidOperationException($"Could not save statistics. Code: {saveCode}");
        }

        private string AppendReason(string originalReason, string additionalReason)
        {
            if (string.IsNullOrWhiteSpace(originalReason)) return additionalReason;
            return $"{originalReason};{additionalReason}";
        }

        private void TrySaveMatchStatistics(GameSession session, GameEndResult result)
        {
            try
            {
                SaveMatchStatistics(session, result);
            }
            catch (ArgumentNullException ex)
            {
                loggerHelper.LogError($"Statistics were not saved (ArgumentNullException) for {session.MatchCode} - {ex.Message}", ex);
                result.Reason = AppendReason(result.Reason, "statistics_null_error");
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Statistics were not saved (InvalidOperationException) for {session.MatchCode} - {ex.Message}", ex);
                result.Reason = AppendReason(result.Reason, "statistics_invalid_operation");
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"Statistics were not saved (SQL) for {session.MatchCode} - {ex.Message}", ex);
                result.Reason = AppendReason(result.Reason, "statistics_sql_error");
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Statistics were not saved (Entity Framework) for {session.MatchCode} - {ex.Message}", ex);
                result.Reason = AppendReason(result.Reason, "statistics_entity_error");
            }
        }

        public void LeaveGame(string matchCode, int userId)
        {
            if (string.IsNullOrWhiteSpace(matchCode)) return;

            var session = gameCoreContext.Sessions.GetSession(matchCode);
            if (session == null) return;

            lock (session.SyncRoot)
            {
                var playerLeaving = RemovePlayerFromSession(session, userId);
                if (playerLeaving == null) return;

                NotifyPlayersPlayerLeft(session, playerLeaving);

                if (session.Players.Count < 2 && !session.IsFinished)
                {
                    EndGameDueToInsufficientPlayers(session);
                }
            }
        }

        private PlayerSession RemovePlayerFromSession(GameSession session, int userId)
        {
            var player = session.Players.FirstOrDefault(p => p.UserId == userId);
            if (player != null && session.RemovePlayer(userId))
            {
                loggerHelper.LogInfo($"Player {player.Nickname} ({userId}) left the match {session.MatchCode}");
                return player;
            }
            return null;
        }

        private void NotifyPlayersPlayerLeft(GameSession session, PlayerSession playerLeaving)
        {
            var dto = new PlayerExpelledDTO
            {
                MatchId = session.MatchCode.GetHashCode(),
                ExpelledUserId = playerLeaving.UserId,
                ExpelledUsername = playerLeaving.Nickname,
                Reason = "PlayerLeft"
            };

            gameNotifier.NotifyPlayerExpelled(dto);
        }

        private void EndGameDueToInsufficientPlayers(GameSession session)
        {
            var result = EndGame(session.MatchCode, GameEndType.Aborted, "Someone left");
        }

        private BodyPartAttachedDTO CreateBodyPartAttachedDTO(AttachBodyPartContext context)
        {
            return new BodyPartAttachedDTO
            {
                MatchCode = context.Session.MatchCode,
                PlayerUserId = context.Player.UserId,
                DinoInstanceId = context.Dino.DinoInstanceId,
                BodyCard = CreateCardDTO(context.Card),
                NewTotalPower = context.Dino.TotalPower
            };
        }

        private CardDrawnDTO CreateCardDrawnDTO(GameSession session, PlayerSession player, CardInGame card)
        {
            return new CardDrawnDTO
            {
                MatchCode = session.MatchCode,
                PlayerUserId = player.UserId,
                Card = CreateCardDTO(card)
            };
        }

        private DinoPlayedDTO CreateDinoHeadPlayedDTO(GameSession session, PlayerSession player, DinoInstance dino)
        {
            return new DinoPlayedDTO
            {
                MatchCode = session.MatchCode,
                PlayerUserId = player.UserId,
                DinoInstanceId = dino.DinoInstanceId,
                HeadCard = CreateCardDTO(dino.HeadCard)
            };
        }

        private BattleResultDTO CreateBattleResultDTO(string matchCode, BattleResult result)
        {
            if (result == null) throw new InvalidOperationException("BattleResult cannot be null.");

            var archCardsDTO = result.ArchCardIds.Select(CardInGame.FromDefinition)
                                                 .Where(card => card != null)
                                                 .Select(card => new CardDTO
                                                 {
                                                     IdCard = card.IdCard,
                                                     Power = card.Power,
                                                     Element = card.Element,
                                                     PartType = card.PartType,
                                                     HasTopJoint = card.HasTopJoint,
                                                     HasBottomJoint = card.HasBottomJoint,
                                                     HasLeftJoint = card.HasLeftJoint,
                                                     HasRightJoint = card.HasRightJoint
                                                 }).ToList();

            var playerPowers = result.PlayerDinos.ToDictionary(
                playerEntry => playerEntry.Key,
                playerEntry => playerEntry.Value.Sum(dino => dino.TotalPower)
            );

            return new BattleResultDTO
            {
                MatchCode = matchCode,
                ArmyType = result.ArmyType,
                ArchPower = result.ArchPower,
                DinosWon = result.DinosWon,
                WinnerUserId = result.Winner?.UserId,
                WinnerUsername = result.Winner?.Nickname,
                WinnerPower = result.WinnerPower,
                PointsAwarded = result.DinosWon && result.Winner != null
                                ? CalculateArchPointsForDTO(result)
                                : 0,
                ArchCards = archCardsDTO,
                PlayerPowers = playerPowers
            };
        }

        private int CalculateArchPointsForDTO(BattleResult result)
        {
            int points = result.ArchCardIds.Count;
            var bossCard = result.Winner?.Dinos.SelectMany(dino => dino.GetAllCards())
                                            .FirstOrDefault(card => card.Element == result.ArmyType);
            if (bossCard != null)
                points += 3;
            return points;
        }

        private void NotifyCardExchange(CardExchangeContext context)
        {
            var dto = new CardExchangedDTO
            {
                MatchCode = context.MatchCode,
                PlayerAUserId = context.PlayerA.UserId,
                PlayerBUserId = context.PlayerB.UserId,
                CardFromPlayerA = CreateCardDTO(context.CardFromA),
                CardFromPlayerB = CreateCardDTO(context.CardFromB)
            };

            gameNotifier.NotifyCardExchanged(dto);
        }

        private CardDTO CreateCardDTO(CardInGame card)
        {
            return new CardDTO
            {
                IdCard = card.IdCard,
                Power = card.Power,
                Element = card.Element,
                PartType = card.PartType,
                HasTopJoint = card.HasTopJoint,
                HasBottomJoint = card.HasBottomJoint,
                HasLeftJoint = card.HasLeftJoint,
                HasRightJoint = card.HasRightJoint
            };
        }

        private void NotifyInitialArchsOnBoard(GameSession session)
        {
            var board = session.CentralBoard;

            foreach (var archId in board.SandArmy)
            {
                var archCard = CardInGame.FromDefinition(archId);
                if (archCard != null)
                {
                    var dto = new ArchAddedToBoardDTO
                    {
                        MatchId = session.MatchCode.GetHashCode(),
                        PlayerUserId = 0,
                        PlayerUsername = "System",
                        ArchCard = CreateCardDTO(archCard),
                        ArmyType = ArmyType.Sand.ToString(),
                        NewArchCount = board.SandArmy.Count
                    };
                    gameNotifier.NotifyArchAddedToBoard(dto);
                }
            }

            foreach (var archId in board.WaterArmy)
            {
                var archCard = CardInGame.FromDefinition(archId);
                if (archCard != null)
                {
                    var dto = new ArchAddedToBoardDTO
                    {
                        MatchId = session.MatchCode.GetHashCode(),
                        PlayerUserId = 0,
                        PlayerUsername = "System",
                        ArchCard = CreateCardDTO(archCard),
                        ArmyType = ArmyType.Water.ToString(),
                        NewArchCount = board.WaterArmy.Count
                    };
                    gameNotifier.NotifyArchAddedToBoard(dto);
                }
            }

            foreach (var archId in board.WindArmy)
            {
                var archCard = CardInGame.FromDefinition(archId);
                if (archCard != null)
                {
                    var dto = new ArchAddedToBoardDTO
                    {
                        MatchId = session.MatchCode.GetHashCode(),
                        PlayerUserId = 0,
                        PlayerUsername = "System",
                        ArchCard = CreateCardDTO(archCard),
                        ArmyType = ArmyType.Wind.ToString(),
                        NewArchCount = board.WindArmy.Count
                    };
                    gameNotifier.NotifyArchAddedToBoard(dto);
                }
            }

            int totalArchs = board.SandArmy.Count + board.WaterArmy.Count + board.WindArmy.Count;
            loggerHelper.LogInfo($"Notified {totalArchs} initial Archs in match {session.MatchCode}");
        }
    }
}