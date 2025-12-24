using ArchsVsDinosServer.BusinessLogic.GameManagement;
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
using System.Web.SessionState;

namespace ArchsVsDinosServer.BusinessLogic
{
    public class GameLogic : IGameLogic
    {
        private readonly ILoggerHelper logger;
        private readonly GameCoreContext core;
        private readonly GameRulesValidator validator;
        private readonly GameEndHandler endHandler;
        private readonly IGameNotifier notifier;
        private readonly IStatisticsManager statisticsManager;

        public GameLogic(GameCoreContext core, ILoggerHelper logger, IGameNotifier notifier, IStatisticsManager statisticsManager)
        {
            this.core = core ?? throw new ArgumentNullException(nameof(core));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.validator = new GameRulesValidator();
            this.endHandler = new GameEndHandler();
            this.notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
            this.statisticsManager = statisticsManager ?? throw new ArgumentNullException(nameof(statisticsManager));
        }
        public bool AttachBodyPart(string matchCode, int userId, AttachBodyPartDTO data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var session = GetActiveSession(matchCode);

            lock (session.SyncRoot)
            {
                var player = GetPlayer(session, userId);
                var dino = validator.FindDinoByHeadCardId(player, data.DinoHeadCardId);
                var card = player.GetCardById(data.CardId);

                if (!validator.IsValidBodyPart(card))
                    throw new InvalidOperationException($"Carta {data.CardId} no es un cuerpo válido.");

                if (!validator.CanAttachBodyPart(card, dino))
                    throw new InvalidOperationException($"No se puede adjuntar la carta {card.IdCard} al Dino {dino.DinoInstanceId}.");

                if (player.Dinos.Any(d => d.GetAllCards().Any(c => c.IdCard == card.IdCard)))
                    throw new InvalidOperationException($"Carta {card.IdCard} ya está usada en otro Dino.");

                if (!dino.TryAddBodyPart(card) || !player.RemoveCard(card))
                    throw new InvalidOperationException($"No se pudo adjuntar o remover la carta {card.IdCard}.");

                logger.LogInfo($"Jugador {userId} adjuntó carta {card.IdCard} al Dino {dino.DinoInstanceId} en {matchCode}.");

                var context = new AttachBodyPartContext
                {
                    Session = session,
                    Player = player,
                    Dino = dino,
                    Card = card
                };

                var dto = CreateBodyPartAttachedDTO(context);
                notifier.NotifyBodyPartAttached(dto);
                return true;
            }
        }
        public CardInGame DrawCard(string matchCode, int userId, int pileIndex)
        {
            var session = GetActiveSession(matchCode);
            var player = GetPlayer(session, userId);

            lock (session.SyncRoot)
            {
                if (!session.ConsumeMoves(1))
                    throw new InvalidOperationException("No quedan movimientos.");

                var drawnIds = session.DrawFromPile(pileIndex, 1);
                if (drawnIds == null || drawnIds.Count == 0) throw new InvalidOperationException("No hay cartas disponibles en el mazo.");

                var card = CardInGame.FromDefinition(drawnIds[0]);
                if (card == null) throw new InvalidOperationException("La carta robada no es válida.");

                if (card.IsArch())
                {
                    session.CentralBoard.AddArchCardToArmy(card);
                }
                else
                {
                    player.AddCard(card);
                }

                logger.LogInfo($"Player {userId} drew card {card.IdCard} from pile {pileIndex} in match {matchCode}");

                var dto = CreateCardDrawnDTO(session, player, card);
                notifier.NotifyCardDrawn(dto);
                return card;
            }
        }
        public bool EndTurn(string matchCode, int userId)
        {
            var session = GetActiveSession(matchCode);
            var player = session.Players.FirstOrDefault(p => p.UserId == userId);
            if (player == null) throw new InvalidOperationException("Jugador no encontrado.");

            lock (session.SyncRoot)
            {
                var nextPlayer = session.Players.OrderBy(p => p.TurnOrder)
                                               .SkipWhile(p => p.UserId != userId)
                                               .Skip(1)
                                               .DefaultIfEmpty(session.Players.OrderBy(p => p.TurnOrder).First())
                                               .First();

                session.EndTurn(nextPlayer.UserId);
                logger.LogInfo($"Turn ended for {userId}. Next: {nextPlayer.UserId} in {matchCode}");

                notifier.NotifyTurnChanged(new TurnChangedDTO
                {
                    MatchCode = matchCode,
                    CurrentPlayerUserId = nextPlayer.UserId,
                    TurnNumber = session.TurnNumber,
                    RemainingTime = TimeSpan.Zero,
                    PlayerScores = session.Players.ToDictionary(p => p.UserId, p => p.Points)
                });


                return true;
            }
        }


        public bool ExchangeCard(string matchCode, int userId, ExchangeCardDTO data)
        {
            var session = GetActiveSession(matchCode);
            var playerA = GetPlayer(session, userId);
            var playerB = GetPlayer(session, data.TargetUserId);

            if (playerA == null || playerB == null)
                return false;

            lock (session.SyncRoot)
            {
                if (!session.ConsumeMoves(1))
                    return false;

                var cardA = playerA.GetCardById(data.OfferedCardId);
                var cardB = playerB.GetCardById(data.RequestedCardId);

                if (cardA == null || cardB == null)
                    return false;

                if (cardA.PartType != cardB.PartType)
                    return false;

                var exchangeContext = new CardExchangeContext
                {
                    MatchCode = matchCode,
                    PlayerA = playerA,
                    PlayerB = playerB,
                    CardFromA = cardA,
                    CardFromB = cardB
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



        public Task<bool> InitializeMatch(string matchCode, List<GamePlayerInitDTO> players)
        {
            if (!IsValidInitialization(matchCode, players))
                return Task.FromResult(false);

            var session = CreateGameSession(matchCode);
            if (session == null)
                return Task.FromResult(false);

            var playerSessions = CreatePlayerSessions(players);

            if (!SetupGame(session, playerSessions))
                return Task.FromResult(false);

            StartFirstTurn(session);

            logger.LogInfo($"InitializeMatch: Match {matchCode} initialized successfully.");

            var initDto = new GameInitializedDTO
            {
                MatchCode = matchCode,
                Players = playerSessions.Select((p, index) => new PlayerInGameDTO
                {
                    UserId = p.UserId,
                    TurnOrder = index + 1
                }).ToList(),
                RemainingCardsInDeck = session.DrawPiles.Sum(p => p.Count)
            };

            notifier.NotifyGameInitialized(initDto);


            return Task.FromResult(true);
        }


        public DinoInstance PlayDinoHead(string matchCode, int userId, int headCardId)
        {
            var session = GetActiveSession(matchCode);
            var player = GetPlayer(session, userId);

            var card = player.GetCardById(headCardId);
            if (!validator.IsValidDinoHead(card)) throw new InvalidOperationException("No es cabeza de dino válida.");

            lock (session.SyncRoot)
            {
                var dino = new DinoInstance(player.GetNextDinoId(), card);
                if (!player.RemoveCard(card)) throw new InvalidOperationException("No se pudo remover carta.");
                player.AddDino(dino);

                var dto = CreateDinoHeadPlayedDTO(session, player, dino);
                notifier.NotifyDinoHeadPlayed(dto);

                return dino;
            }
        }

        public Task<bool> Provoke(string matchCode, int userId, ArmyType targetArmy)
        {
            var session = GetActiveSession(matchCode);
            var player = GetPlayer(session, userId);

            if (!validator.CanProvoke(session, userId))
                throw new InvalidOperationException("No se puede provocar, no hay movimientos suficientes.");

            lock (session.SyncRoot)
            {
                session.ConsumeMoves(session.RemainingMoves);

                var battleResolver = new BattleResolver(new ServiceDependencies());
                var result = battleResolver.ResolveBattle(session, targetArmy);

                var battleResultDto = CreateBattleResultDTO(matchCode, result);

                var provokedDto = new ArchArmyProvokedDTO
                {
                    MatchCode = matchCode,
                    ProvokerUserId = userId,
                    ArmyType = targetArmy,
                    BattleResult = battleResultDto
                };

                notifier.NotifyArchArmyProvoked(provokedDto);

                DiscardArmy(session, targetArmy);
                DiscardDinos(session);

                logger.LogInfo($"Jugador {userId} provocó ejército {targetArmy} en {matchCode}. " +
                               $"DinosWon: {result?.DinosWon}, Winner: {result?.Winner?.Nickname ?? "Nadie"}");

                return Task.FromResult(true);
            }
        }
        private bool IsValidInitialization(string matchCode, List<GamePlayerInitDTO> players)
        {
            if (string.IsNullOrWhiteSpace(matchCode) || players == null || players.Count < 2 || players.Count > 4)
            {
                logger.LogWarning("InitializeMatch: Invalid parameters.");
                return false;
            }

            if (core.Sessions.SessionExists(matchCode))
            {
                logger.LogWarning($"InitializeMatch: Session {matchCode} already exists.");
                return false;
            }

            return true;
        }

        private GameSession CreateGameSession(string matchCode)
        {
            if (!core.Sessions.CreateSession(matchCode))
            {
                logger.LogWarning($"CreateGameSession: Failed to create session {matchCode}");
                return null;
            }

            var session = core.Sessions.GetSession(matchCode);
            if (session == null)
            {
                logger.LogWarning($"CreateGameSession: Failed to retrieve session {matchCode}");
                core.Sessions.RemoveSession(matchCode);
            }

            return session;
        }

        private List<PlayerSession> CreatePlayerSessions(List<GamePlayerInitDTO> players)
        {
            return players
                .Select(p => new PlayerSession(p.UserId, p.Nickname, callback: null))
                .ToList();
        }

        private bool SetupGame(GameSession session, List<PlayerSession> players)
        {
            if (!core.Setup.InitializeGameSession(session, players))
            {
                logger.LogWarning($"SetupGame: Setup failed for match {session.MatchCode}");
                core.Sessions.RemoveSession(session.MatchCode);
                return false;
            }

            return true;
        }

        private void StartFirstTurn(GameSession session)
        {
            var firstPlayer = core.Setup.SelectFirstPlayer(session);
            if (firstPlayer != null)
            {
                session.StartTurn(firstPlayer.UserId);
            }
        }

        private GameSession GetActiveSession(string matchCode)
        {
            if (string.IsNullOrWhiteSpace(matchCode))
                throw new ArgumentException("matchCode no puede ser nulo o vacío.", nameof(matchCode));

            var session = core.Sessions.GetSession(matchCode);
            if (session == null)
                throw new InvalidOperationException($"No se encontró la sesión con matchCode {matchCode}.");

            if (!session.IsStarted || session.IsFinished)
                throw new InvalidOperationException("El juego no está activo.");

            return session;
        }

        private PlayerSession GetPlayer(GameSession session, int userId)
        {
            var player = session.Players.FirstOrDefault(p => p.UserId == userId);
            if (player == null)
                throw new InvalidOperationException($"Jugador {userId} no encontrado en la sesión {session.MatchCode}.");

            if (session.CurrentTurn != userId)
                throw new InvalidOperationException("No es el turno del jugador.");

            if (!session.ConsumeMoves(1))
                throw new InvalidOperationException("No quedan movimientos disponibles.");

            return player;
        }

        private void DiscardDinos(GameSession session)
        {
            foreach (var p in session.Players)
            {
                var dinoCards = p.Dinos.SelectMany(d => d.GetAllCards()).Select(c => c.IdCard).ToList();
                p.ClearDinos();
                session.AddToDiscard(dinoCards);
            }
        }

        private void DiscardArmy(GameSession session, ArmyType army)
        {
            var discardedArchs = session.CentralBoard.ClearArmy(army);

            if (session.CentralBoard.SupremeBossCard != null &&
                session.CentralBoard.SupremeBossCard.Element == army)
            {
                discardedArchs.Add(session.CentralBoard.RemoveSupremeBoss().IdCard);
            }

            session.AddToDiscard(discardedArchs);
        }

        public GameEndResult EndGame(string matchCode, GameEndType gameType, string reason)
        {
            if (string.IsNullOrWhiteSpace(matchCode))
                throw new ArgumentException(nameof(matchCode));

            var session = core.Sessions.GetSession(matchCode)
                ?? throw new InvalidOperationException($"Sesión {matchCode} no encontrada.");

            lock (session.SyncRoot)
            {
                GameEndResult result;

                if (gameType == GameEndType.Finished)
                {
                    if (!endHandler.ShouldGameEnd(session))
                        throw new InvalidOperationException("El juego aún no puede finalizar.");

                    result = endHandler.EndGame(session)
                        ?? throw new InvalidOperationException("No se pudo calcular el resultado.");

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
                core.Sessions.RemoveSession(matchCode);

                notifier.NotifyGameEnded(new GameEndedDTO
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
                throw new ArgumentNullException(nameof(session), "Session no puede ser nula.");
            if (result == null)
                throw new ArgumentNullException(nameof(result), "GameEndResult no puede ser nulo.");

            if (statisticsManager == null)
                throw new InvalidOperationException("StatisticsManager no está inicializado.");

            var matchResultDto = new MatchResultDTO
            {
                MatchId = session.MatchCode,
                MatchDate = session.StartTime ?? DateTime.UtcNow,

                PlayerResults = session.Players
                .Where(p => p.UserId > 0)  
                .Select(p =>
                {
                    int archaeologistsEliminated = p.Dinos.Sum(d => d.ArchaeologistsEliminated);
                    int supremeBossesEliminated = p.Dinos.Sum(d => d.SupremeBossesEliminated);

                    return new PlayerMatchResultDTO
                    {
                        UserId = p.UserId,
                        Points = p.Points,
                        IsWinner = result.Winner?.UserId == p.UserId,
                        ArchaeologistsEliminated = archaeologistsEliminated,
                        SupremeBossesEliminated = supremeBossesEliminated
                    };
                }).ToList()
            };

            if (matchResultDto.PlayerResults.Count == 0)
            {
                logger.LogInfo($"No registered players in match {session.MatchCode}, skipping statistics save");
                return;
            }

            var saveCode = statisticsManager.SaveMatchStatistics(matchResultDto);
            if (saveCode != SaveMatchResultCode.Success)
                throw new InvalidOperationException($"No se pudieron guardar estadísticas. Código: {saveCode}");
        }

        private string AppendReason(string original, string addition)
        {
            if (string.IsNullOrWhiteSpace(original)) return addition;
            return $"{original};{addition}";
        }

        private void TrySaveMatchStatistics(GameSession session, GameEndResult result)
        {
            try
            {
                SaveMatchStatistics(session, result);
            }
            catch (ArgumentNullException ex)
            {
                logger.LogError($"Estadísticas no se guardaron (ArgumentNullException) para {session.MatchCode} - {ex.Message}", ex);
                result.Reason = AppendReason(result.Reason, "statistics_null_error");
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError($"Estadísticas no se guardaron (InvalidOperationException) para {session.MatchCode} - {ex.Message}", ex);
                result.Reason = AppendReason(result.Reason, "statistics_invalid_operation");
            }
            catch (SqlException ex)
            {
                logger.LogError($"Estadísticas no se guardaron (SQL) para {session.MatchCode} - {ex.Message}", ex);
                result.Reason = AppendReason(result.Reason, "statistics_sql_error");
            }
            catch (EntityException ex)
            {
                logger.LogError($"Estadísticas no se guardaron (Entity Framework) para {session.MatchCode} - {ex.Message}", ex);
                result.Reason = AppendReason(result.Reason, "statistics_entity_error");
            }
        }

        

        public void LeaveGame(string matchCode, int userId)
        {
            if (string.IsNullOrWhiteSpace(matchCode)) return;

            var session = core.Sessions.GetSession(matchCode);
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
                logger.LogInfo($"Jugador {player.Nickname} ({userId}) salió de la partida {session.MatchCode}");
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

            notifier.NotifyPlayerExpelled(dto);
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
                Card = new CardDTO
                {
                    IdCard = card.IdCard,
                    Power = card.Power,
                    Element = card.Element,
                    PartType = card.PartType,
                    HasTopJoint = card.HasTopJoint,
                    HasBottomJoint = card.HasBottomJoint,
                    HasLeftJoint = card.HasLeftJoint,
                    HasRightJoint = card.HasRightJoint
                }
            };
        }

        private DinoPlayedDTO CreateDinoHeadPlayedDTO(GameSession session, PlayerSession player, DinoInstance dino)
        {
            return new DinoPlayedDTO
            {
                MatchCode = session.MatchCode,
                PlayerUserId = player.UserId,
                DinoInstanceId = dino.DinoInstanceId,
                HeadCard = new CardDTO
                {
                    IdCard = dino.HeadCard.IdCard,
                    Power = dino.HeadCard.Power,
                    Element = dino.HeadCard.Element,
                    PartType = dino.HeadCard.PartType,
                    HasTopJoint = dino.HeadCard.HasTopJoint,
                    HasBottomJoint = dino.HeadCard.HasBottomJoint,
                    HasLeftJoint = dino.HeadCard.HasLeftJoint,
                    HasRightJoint = dino.HeadCard.HasRightJoint
                }
            };
        }

        private BattleResultDTO CreateBattleResultDTO(string matchCode, BattleResult result)
        {
            if (result == null) throw new InvalidOperationException("BattleResult no puede ser null.");

            var archCardsDTO = result.ArchCardIds.Select(CardInGame.FromDefinition)
                                                 .Where(c => c != null)
                                                 .Select(c => new CardDTO
                                                 {
                                                     IdCard = c.IdCard,
                                                     Power = c.Power,
                                                     Element = c.Element,
                                                     PartType = c.PartType,
                                                     HasTopJoint = c.HasTopJoint,
                                                     HasBottomJoint = c.HasBottomJoint,
                                                     HasLeftJoint = c.HasLeftJoint,
                                                     HasRightJoint = c.HasRightJoint
                                                 }).ToList();

            var playerPowers = result.PlayerDinos.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Sum(d => d.TotalPower)
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
            var bossCard = result.Winner?.Dinos.SelectMany(d => d.GetAllCards())
                                .FirstOrDefault(c => c.Element == result.ArmyType);
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

            notifier.NotifyCardExchanged(dto);
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
    }
}
