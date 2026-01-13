using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO.Game_DTO;
using Contracts.DTO.Game_DTO.Enums;
using Contracts.DTO.Game_DTO.State;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, InstanceContextMode = InstanceContextMode.PerSession)]
    public class GameManager : IGameManager
    {
        private const string DISCONNECT_REASON_CHANNEL_FAULTED = "WCF channel faulted.";
        private const string DISCONNECT_REASON_CHANNEL_CLOSED = "WCF channel closed.";

        private static readonly ConcurrentDictionary<string, byte> disconnectGuards =
            new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        private readonly IGameLogic gameLogic;
        private readonly ILoggerHelper logger;

        public GameManager()
        {
            logger = ServiceContext.Logger;
            gameLogic = ServiceContext.GameLogic;
        }

        public void ConnectToGame(string matchCode, int userId)
        {
            if (string.IsNullOrWhiteSpace(matchCode) || userId <= 0)
            {
                return;
            }

            if (OperationContext.Current == null)
            {
                logger.LogWarning("ConnectToGame called without OperationContext.");
                return;
            }

            try
            {
                IGameManagerCallback callback =
                    OperationContext.Current.GetCallbackChannel<IGameManagerCallback>();

                GameCallbackRegistry.Instance.RegisterCallback(userId, matchCode, callback);

                AttachDisconnectHandlers(matchCode, userId, callback);

                gameLogic.ConnectPlayerToGame(matchCode, userId, callback);

                logger.LogInfo(string.Format("User {0} connected to match {1}", userId, matchCode));

                Task.Run(() => AttemptStateRecovery(matchCode, userId, callback));
            }
            catch (Exception ex)
            {
                logger.LogError(string.Format("Error in ConnectToGame for user {0}", userId), ex);
            }
        }

        private void AttachDisconnectHandlers(string matchCode, int userId, IGameManagerCallback callback)
        {
            ICommunicationObject comm = callback as ICommunicationObject;
            if (comm == null)
            {
                return;
            }

            comm.Faulted += (s, e) => HandleClientDisconnected(matchCode, userId, DISCONNECT_REASON_CHANNEL_FAULTED);
            comm.Closed += (s, e) => HandleClientDisconnected(matchCode, userId, DISCONNECT_REASON_CHANNEL_CLOSED);
        }

        private void HandleClientDisconnected(string matchCode, int userId, string reason)
        {
            if (string.IsNullOrWhiteSpace(matchCode) || userId <= 0)
            {
                return;
            }

            string guardKey = string.Format("{0}:{1}", matchCode.Trim(), userId);

            if (!disconnectGuards.TryAdd(guardKey, 0))
            {
                return;
            }

            try
            {
                GameCallbackRegistry.Instance.UnregisterPlayer(userId);

                try
                {
                    gameLogic.LeaveGame(matchCode, userId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(string.Format(
                        "LeaveGame failed for user {0} match {1}: {2}",
                        userId,
                        matchCode,
                        ex.Message));
                }

                TryForceLogout(matchCode, userId, reason);

                logger.LogInfo(string.Format(
                    "Disconnected user {0} removed from match {1}. Reason: {2}",
                    userId,
                    matchCode,
                    reason));
            }
            catch (Exception ex)
            {
                logger.LogError(string.Format(
                    "HandleClientDisconnected failed for user {0} match {1}",
                    userId,
                    matchCode), ex);
            }
            finally
            {
                disconnectGuards.TryRemove(guardKey, out byte ignored);
            }
        }

        private void TryForceLogout(string matchCode, int userId, string reason)
        {
            try
            {
                var session = ServiceContext.GameSessions.GetSession(matchCode);
                if (session == null)
                {
                    return;
                }

                string username = string.Empty;

                lock (session.SyncRoot)
                {
                    var player = session.Players.FirstOrDefault(p => p.UserId == userId);
                    if (player != null)
                    {
                        username = (player.Nickname ?? string.Empty).Trim();
                    }
                }

                if (string.IsNullOrWhiteSpace(username))
                {
                    return;
                }

                SessionManager.Instance.RemoveUser(username);

                logger.LogInfo(string.Format(
                    "ForceLogout: removed '{0}' due to disconnect in match {1}. Reason: {2}",
                    username,
                    matchCode,
                    reason));
            }
            catch (Exception ex)
            {
                logger.LogWarning(string.Format(
                    "ForceLogout failed for userId {0} match {1}: {2}",
                    userId,
                    matchCode,
                    ex.Message));
            }
        }

        private void AttemptStateRecovery(string matchCode, int userId, IGameManagerCallback callback)
        {
            try
            {
                var session = ServiceContext.GameSessions.GetSession(matchCode);

                GameStartedDTO recoveryDto = null;

                if (session != null && session.IsStarted && !session.IsFinished)
                {
                    lock (session.SyncRoot)
                    {
                        var player = session.Players.FirstOrDefault(p => p.UserId == userId);

                        if (player != null)
                        {
                            recoveryDto = new GameStartedDTO
                            {
                                MatchId = session.MatchCode.GetHashCode(),
                                FirstPlayerUserId = session.CurrentTurn,
                                FirstPlayerUsername = string.Empty,
                                MyUserId = userId,
                                StartTime = session.StartTime ?? DateTime.UtcNow,
                                MatchEndTime = session.MatchEndTime,
                                TurnEndTime = session.TurnEndTime,
                                DrawDeckCount = session.DrawDeck.Count,
                                PlayersHands = new List<PlayerHandDTO>
                                {
                                    new PlayerHandDTO
                                    {
                                        UserId = userId,
                                        Cards = player.Hand.Select(c => MapCardToDTO(c)).ToList()
                                    }
                                },
                                InitialBoard = MapBoardToDTO(session.CentralBoard)
                            };
                        }
                    }

                    if (recoveryDto != null)
                    {
                        try
                        {
                            callback.OnGameStarted(recoveryDto);
                            logger.LogInfo(string.Format(
                                "[RECOVERY] State sent to reconnected user {0} in {1}",
                                userId,
                                matchCode));
                        }
                        catch (CommunicationException ex)
                        {
                            logger.LogWarning(string.Format(
                                "[RECOVERY] Could not send recovery to user {0}: {1}",
                                userId,
                                ex.Message));
                        }
                        catch (TimeoutException ex)
                        {
                            logger.LogWarning(string.Format(
                                "[RECOVERY] Timeout sending recovery to user {0}: {1}",
                                userId,
                                ex.Message));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(string.Format("State recovery failed for user {0}: {1}", userId, ex.Message));
            }
        }

        public void LeaveGame(string matchCode, int userId)
        {
            try
            {
                gameLogic.LeaveGame(matchCode, userId);
            }
            catch (Exception ex)
            {
                logger.LogError(string.Format("Error in LeaveGame for user {0}", userId), ex);
            }
            finally
            {
                GameCallbackRegistry.Instance.UnregisterPlayer(userId);
                logger.LogInfo(string.Format("User {0} removed from callbacks/match {1}", userId, matchCode));
            }
        }

        public Task<DrawCardResultCode> DrawCard(string matchCode, int userId)
        {
            try
            {
                gameLogic.DrawCard(matchCode, userId);
                return Task.FromResult(DrawCardResultCode.DrawCard_Success);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(string.Format("DrawCard logic denied: {0}", ex.Message));

                if (ex.Message.Contains("turn"))
                {
                    return Task.FromResult(DrawCardResultCode.DrawCard_NotYourTurn);
                }

                if (ex.Message.Contains("moves"))
                {
                    return Task.FromResult(DrawCardResultCode.DrawCard_AlreadyDrewThisTurn);
                }

                if (ex.Message.Contains("empty"))
                {
                    return Task.FromResult(DrawCardResultCode.DrawCard_DrawPileEmpty);
                }

                return Task.FromResult(DrawCardResultCode.DrawCard_InvalidParameter);
            }
            catch (Exception ex)
            {
                logger.LogError("DrawCard unexpected error", ex);
                return Task.FromResult(DrawCardResultCode.DrawCard_UnexpectedError);
            }
        }

        public Task<PlayCardResultCode> PlayDinoHead(string matchCode, int userId, int cardId)
        {
            try
            {
                gameLogic.PlayDinoHead(matchCode, userId, cardId);
                return Task.FromResult(PlayCardResultCode.PlayCard_Success);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(string.Format("PlayDinoHead logic denied: {0}", ex.Message));

                if (ex.Message.Contains("turn"))
                {
                    return Task.FromResult(PlayCardResultCode.PlayCard_NotYourTurn);
                }

                if (ex.Message.Contains("moves"))
                {
                    return Task.FromResult(PlayCardResultCode.PlayCard_AlreadyPlayedTwoCards);
                }

                if (ex.Message.Contains("Card not found"))
                {
                    return Task.FromResult(PlayCardResultCode.PlayCard_CardNotInHand);
                }

                if (ex.Message.Contains("valid"))
                {
                    return Task.FromResult(PlayCardResultCode.PlayCard_InvalidDinoHead);
                }

                return Task.FromResult(PlayCardResultCode.PlayCard_UnexpectedError);
            }
            catch (Exception ex)
            {
                logger.LogError("PlayDinoHead unexpected error", ex);
                return Task.FromResult(PlayCardResultCode.PlayCard_UnexpectedError);
            }
        }

        public Task<PlayCardResultCode> AttachBodyPartToDino(string matchCode, int userId, AttachBodyPartDTO attachmentData)
        {
            try
            {
                bool success = gameLogic.AttachBodyPart(matchCode, userId, attachmentData);

                return Task.FromResult(success
                    ? PlayCardResultCode.PlayCard_Success
                    : PlayCardResultCode.PlayCard_InvalidDinoHead);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(string.Format("AttachBodyPart logic denied: {0}", ex.Message));

                if (ex.Message.Contains("turn"))
                {
                    return Task.FromResult(PlayCardResultCode.PlayCard_NotYourTurn);
                }

                if (ex.Message.Contains("moves"))
                {
                    return Task.FromResult(PlayCardResultCode.PlayCard_AlreadyPlayedTwoCards);
                }

                if (ex.Message.Contains("attach"))
                {
                    return Task.FromResult(PlayCardResultCode.PlayCard_ArmyTypeMismatch);
                }

                return Task.FromResult(PlayCardResultCode.PlayCard_UnexpectedError);
            }
            catch (Exception ex)
            {
                logger.LogError("AttachBodyPart unexpected error", ex);
                return Task.FromResult(PlayCardResultCode.PlayCard_UnexpectedError);
            }
        }

        public Task<DrawCardResultCode> TakeCardFromDiscardPile(string matchCode, int userId, int cardId)
        {
            try
            {
                gameLogic.TakeCardFromDiscardPile(matchCode, userId, cardId);
                return Task.FromResult(DrawCardResultCode.DrawCard_Success);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(string.Format("TakeCardFromDiscard logic denied: {0}", ex.Message));

                if (ex.Message.Contains("turn"))
                {
                    return Task.FromResult(DrawCardResultCode.DrawCard_NotYourTurn);
                }

                if (ex.Message.Contains("moves"))
                {
                    return Task.FromResult(DrawCardResultCode.DrawCard_AlreadyDrewThisTurn);
                }

                if (ex.Message.Contains("not found"))
                {
                    return Task.FromResult(DrawCardResultCode.DrawCard_InvalidDrawPile);
                }

                return Task.FromResult(DrawCardResultCode.DrawCard_InvalidParameter);
            }
            catch (Exception ex)
            {
                logger.LogError("TakeCardFromDiscard unexpected error", ex);
                return Task.FromResult(DrawCardResultCode.DrawCard_UnexpectedError);
            }
        }

        public async Task<ProvokeResultCode> ProvokeArchArmy(string matchCode, int userId, ArmyType armyType)
        {
            try
            {
                await gameLogic.Provoke(matchCode, userId, armyType);
                return ProvokeResultCode.Provoke_Success;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(string.Format("Provoke logic denied: {0}", ex.Message));

                if (ex.Message.Contains("turn"))
                {
                    return ProvokeResultCode.Provoke_NotYourTurn;
                }

                if (ex.Message.Contains("moves"))
                {
                    return ProvokeResultCode.Provoke_AlreadyTookAction;
                }

                return ProvokeResultCode.Provoke_UnexpectedError;
            }
            catch (Exception ex)
            {
                logger.LogError("Provoke unexpected error", ex);
                return ProvokeResultCode.Provoke_UnexpectedError;
            }
        }

        public Task<EndTurnResultCode> EndTurn(string matchCode, int userId)
        {
            try
            {
                bool success = gameLogic.EndTurn(matchCode, userId);
                return Task.FromResult(success ? EndTurnResultCode.EndTurn_Success : EndTurnResultCode.EndTurn_NotYourTurn);
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                logger.LogError("CRITICAL: DB Failed saving stats during EndTurn", ex);
                return Task.FromResult(EndTurnResultCode.EndTurn_DatabaseError);
            }
            catch (System.Data.Entity.Core.EntityException ex)
            {
                logger.LogError("CRITICAL: EF Failed saving stats during EndTurn", ex);
                return Task.FromResult(EndTurnResultCode.EndTurn_DatabaseError);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(string.Format("EndTurn logic denied: {0}", ex.Message));
                return Task.FromResult(EndTurnResultCode.EndTurn_NotYourTurn);
            }
            catch (Exception ex)
            {
                logger.LogError("EndTurn unexpected error", ex);
                return Task.FromResult(EndTurnResultCode.EndTurn_UnexpectedError);
            }
        }

        private CardDTO MapCardToDTO(ArchsVsDinosServer.BusinessLogic.GameManagement.Cards.CardInGame card)
        {
            if (card == null)
            {
                return null;
            }

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

        private CentralBoardDTO MapBoardToDTO(CentralBoard board)
        {
            return new CentralBoardDTO
            {
                SandArmyCount = board.SandArmy.Count,
                WaterArmyCount = board.WaterArmy.Count,
                WindArmyCount = board.WindArmy.Count,
                SandArmy = board.SandArmy
                    .Select(id => ArchsVsDinosServer.BusinessLogic.GameManagement.Cards.CardInGame.FromDefinition(id))
                    .Select(MapCardToDTO)
                    .ToList(),
                WaterArmy = board.WaterArmy
                    .Select(id => ArchsVsDinosServer.BusinessLogic.GameManagement.Cards.CardInGame.FromDefinition(id))
                    .Select(MapCardToDTO)
                    .ToList(),
                WindArmy = board.WindArmy
                    .Select(id => ArchsVsDinosServer.BusinessLogic.GameManagement.Cards.CardInGame.FromDefinition(id))
                    .Select(MapCardToDTO)
                    .ToList(),
            };
        }
    }
}