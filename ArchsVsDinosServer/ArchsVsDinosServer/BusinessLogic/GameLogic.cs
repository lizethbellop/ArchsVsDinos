using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Game_DTO;
using Contracts.DTO.Game_DTO.Enums;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public GameLogic(GameCoreContext core, ILoggerHelper logger)
        {
            this.core = core ?? throw new ArgumentNullException(nameof(core));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.validator = new GameRulesValidator();
            this.endHandler = new GameEndHandler();
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
                if (drawnIds == null || drawnIds.Count == 0) return null;

                var card = CardInGame.FromDefinition(drawnIds[0]);
                if (card == null) return null;

                if (card.IsArch())
                {
                    session.CentralBoard.AddArchCardToArmy(card);
                }
                else
                {
                    player.AddCard(card);
                }

                logger.LogInfo($"Player {userId} drew card {card.IdCard} from pile {pileIndex} in match {matchCode}");
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
                return true;
            }
        }


        public CardInGame ExchangeCard(string matchCode, int userId, ExchangeCardDTO data)
        {
            var session = GetActiveSession(matchCode);
            var player = GetPlayer(session, userId);

            lock (session.SyncRoot)
            {
                if (!session.ConsumeMoves(1)) return null;

                var cardToDiscard = player.RemoveCardById(data.CardIdToDiscard);
                if (cardToDiscard == null)
                {
                    session.RestoreMoves(1);
                    return null;
                }

                session.AddToDiscard(cardToDiscard.IdCard);

                var drawnIds = session.DrawFromPile(data.PileIndex, 1);
                if (drawnIds == null || drawnIds.Count == 0) return null;

                var newCard = CardInGame.FromDefinition(drawnIds[0]);
                if (newCard != null) player.AddCard(newCard);

                logger.LogInfo($"Jugador {userId} intercambió carta {cardToDiscard.IdCard} por {newCard?.IdCard} en {matchCode}");
                return newCard;
            }
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

                int archPower = session.CentralBoard.GetArmyPower(targetArmy);
                int dinoPower = player.Dinos.Sum(d => d.TotalPower);

                bool playerWins = dinoPower >= archPower;
                int pointsGained = 0;
                if (playerWins)
                {
                    pointsGained = archPower + (session.CentralBoard.SupremeBossCard?.Element == targetArmy ? 3 : 0);
                    player.Points += pointsGained;
                }

                DiscardDinos(session);
                DiscardArmy(session, targetArmy);

                logger.LogInfo($"Jugador {userId} provocó ejército {targetArmy} en {matchCode}. DinoPower:{dinoPower}, ArchPower:{archPower}, Points:{pointsGained}");
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

        public GameEndResult EndGame(string matchCode)
        {
            var session = core.Sessions.GetSession(matchCode);
            if (session == null)
            {
                logger.LogWarning($"EndGame: Session {matchCode} not found.");
                return null;
            }

            lock (session.SyncRoot)
            {
                if (!endHandler.ShouldGameEnd(session))
                {
                    logger.LogInfo($"EndGame: Game {matchCode} is not finished yet.");
                    return null;
                }

                var result = endHandler.EndGame(session);

                foreach (var player in session.Players)
                {
                    GameCallbackRegistry.Instance.UnregisterCallback(player.UserId);
                }

                core.Sessions.RemoveSession(matchCode);

                logger.LogInfo($"EndGame: Game {matchCode} ended. Winner: {result.Winner?.Nickname}");
                return result;
            }
        }

        public bool ConnectToGame(string matchCode, int userId, IGameManagerCallback callback)
        {
            var session = core.Sessions.GetSession(matchCode);
            if (session == null)
            {
                logger.LogWarning($"ConnectToGame: Session {matchCode} not found.");
                return false;
            }

            lock (session.SyncRoot)
            {
                GameCallbackRegistry.Instance.RegisterCallback(userId, callback);
                logger.LogInfo($"ConnectToGame: User {userId} connected to {matchCode}.");
                return true;
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

                UnregisterPlayerCallback(userId);
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

        private void UnregisterPlayerCallback(int userId)
        {
            GameCallbackRegistry.Instance.UnregisterCallback(userId);
        }

        private void NotifyPlayersPlayerLeft(GameSession session, PlayerSession playerLeaving)
        {
            foreach (var player in session.Players)
            {
                var callback = GameCallbackRegistry.Instance.GetCallback(player.UserId);
                callback?.OnPlayerExpelled(new PlayerExpelledDTO
                {
                    MatchId = session.MatchCode.GetHashCode(),
                    ExpelledUserId = playerLeaving.UserId,
                    ExpelledUsername = playerLeaving.Nickname,
                    Reason = "PlayerLeft"
                });
            }
        }

        private void EndGameDueToInsufficientPlayers(GameSession session)
        {
            var result = EndGame(session.MatchCode);

            foreach (var player in session.Players)
            {
                var callback = GameCallbackRegistry.Instance.GetCallback(player.UserId);
                callback?.OnGameEnded(new GameEndedDTO
                {
                    MatchId = session.MatchCode.GetHashCode(),
                    WinnerUserId = result.Winner?.UserId ?? 0,
                    WinnerUsername = result.Winner?.Nickname ?? string.Empty,
                    Reason = "NotEnoughPlayers",
                    WinnerPoints = result.WinnerPoints
                });
            }
        }

    }
}
