/*using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Game_DTO;
using Contracts.DTO.Game_DTO.Enums;
using Contracts.DTO.Game_DTO.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.PerSession)]
    public class GameManager : IGameManager
    {
        private readonly IGameLogic gameLogic;
        private readonly ILoggerHelper logger;

        public GameManager()
        {
            logger = ServiceContext.Logger;
            gameLogic = ServiceContext.GameLogic;
        }
        public void AttachBodyPartToDino(string matchCode, int userId, AttachBodyPartDTO attachmentData)
        {
            ExecuteSafe(() =>
            {
                gameLogic.AttachBodyPart(matchCode, userId, attachmentData);
            });
        }

        public void ConnectToGame(string matchCode, int userId)
        {
            if (string.IsNullOrWhiteSpace(matchCode)) throw new ArgumentException(nameof(matchCode));

            var callback = OperationContext.Current.GetCallbackChannel<IGameManagerCallback>();
            GameCallbackRegistry.Instance.RegisterCallback(userId, callback);
            logger.LogInfo($"User {userId} connected to match {matchCode}");

            try
            {
                var session = ServiceContext.GameSessions.GetSession(matchCode);

                if (session != null && session.Players.Any(player => player.Hand.Count > 0))
                {
                    lock (session.SyncRoot)
                    {
                    var player = session.Players.FirstOrDefault(playerSelected => playerSelected.UserId == userId);
                    if (player != null && player.Hand.Count > 0)
                    {
                        int currentDrawDeckCount = session.DrawDeck.Count;

                        var recoveryDto = new GameStartedDTO
                        {
                            MatchId = session.MatchCode.GetHashCode(),
                            FirstPlayerUserId = session.CurrentTurn,
                            FirstPlayerUsername = string.Empty,
                            MyUserId = userId,
                            StartTime = DateTime.UtcNow,
                            PlayersHands = new List<PlayerHandDTO>
                            {
                                new PlayerHandDTO
                                {
                                    UserId = userId,
                                    Cards = player.Hand
                                    .Select(card => MapCardToDTO(card)) 
                                    .Where(dto => dto != null)          
                                    .ToList()
                                }
                            },
                            InitialBoard = MapBoardToDTO(session.CentralBoard),
                            DrawDeckCount = currentDrawDeckCount
                        };

                        Task.Run(() =>
                        {
                            try
                            {
                                callback.OnGameStarted(recoveryDto);
                                logger.LogInfo($"[RECOVERY] DrawDeck state sent to {userId} (Async).");
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning($"Error sending async recovery to {userId}: {ex.Message}");
                            }
                        });
                    }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Error recovering state: {ex.Message}");
            }
        }

        public void DrawCard(string matchCode, int userId)
        {
            ExecuteSafe(() =>
            {
                gameLogic.DrawCard(matchCode, userId);
            });
        }
        public void EndTurn(string matchCode, int userId)
        {
            ExecuteSafe(() =>
            {
                gameLogic.EndTurn(matchCode, userId);
            });
        }

        public void LeaveGame(string matchCode, int userId)
        {
            try
            {
                gameLogic.LeaveGame(matchCode, userId);
            }
            finally
            {
                GameCallbackRegistry.Instance.UnregisterCallback(userId);
                logger.LogInfo($"User {userId} left match {matchCode}");
            }
        }

        public void PlayDinoHead(string matchCode, int userId, int cardId)
        {
            ExecuteSafe(() =>
            {
                gameLogic.PlayDinoHead(matchCode, userId, cardId);
            });
        }

        public void ProvokeArchArmy(string matchCode, int userId, ArmyType armyType)
        {
            ExecuteSafe(() =>
            {
                gameLogic.Provoke(matchCode, userId, armyType);
            });
        }

        public void TakeCardFromDiscardPile(string matchCode, int userId, int cardId)
        {
            ExecuteSafe(() =>
            {
                gameLogic.TakeCardFromDiscardPile(matchCode, userId, cardId);
            });
        }

        private void ExecuteSafe(Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                throw CreateFault(GameFaultCodes.InvalidParameter, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                throw CreateFault(GameFaultCodes.InvalidCard, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError("Unexpected error", ex);
                throw CreateFault(GameFaultCodes.InternalError, "Unexpected server error");
            }
        }

        private FaultException<GameFault> CreateFault(string code, string detail)
        {
            return new FaultException<GameFault>(
                new GameFault
                {
                    Code = code,
                    Detail = detail
                },
                new FaultReason(code)
            );
        }

        private CardDTO MapCardToDTO(CardInGame card)
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
                    .Select(id => CardInGame.FromDefinition(id)) 
                    .Where(c => c != null)
                    .Select(c => MapCardToDTO(c)) 
                    .ToList(),

                WaterArmy = board.WaterArmy
                    .Select(id => CardInGame.FromDefinition(id))
                    .Where(c => c != null)
                    .Select(c => MapCardToDTO(c))
                    .ToList(),

                WindArmy = board.WindArmy
                    .Select(id => CardInGame.FromDefinition(id))
                    .Where(c => c != null)
                    .Select(c => MapCardToDTO(c))
                    .ToList()
            };
        }

    }
}*/

using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO; 
using Contracts.DTO.Game_DTO;
using Contracts.DTO.Game_DTO.Enums;
using Contracts.DTO.Game_DTO.State;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic; 
using System.Linq; 
using System.ServiceModel;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.PerSession)]
    public class GameManager : IGameManager
    {
        private readonly IGameLogic gameLogic;
        private readonly ILoggerHelper logger;

        public GameManager()
        {
            logger = ServiceContext.Logger;
            gameLogic = ServiceContext.GameLogic;
        }

        public void ConnectToGame(string matchCode, int userId)
        {
            if (string.IsNullOrWhiteSpace(matchCode)) return;

            try
            {
                var callback = OperationContext.Current.GetCallbackChannel<IGameManagerCallback>();
                GameCallbackRegistry.Instance.RegisterCallback(userId, callback);
                logger.LogInfo($"User {userId} connected to match {matchCode}");

                Task.Run(() => AttemptStateRecovery(matchCode, userId, callback));
            }
            catch (Exception ex)
            {
                logger.LogError($"Error in ConnectToGame for user {userId}", ex);
            }
        }

        private void AttemptStateRecovery(string matchCode, int userId, IGameManagerCallback callback)
        {
            try
            {
                var session = ServiceContext.GameSessions.GetSession(matchCode);

                if (session != null && session.IsStarted && !session.IsFinished)
                {
                    lock (session.SyncRoot)
                    {
                        var player = session.Players.FirstOrDefault(p => p.UserId == userId);

                        if (player != null)
                        {
                            var recoveryDto = new GameStartedDTO
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

                            callback.OnGameStarted(recoveryDto);
                            logger.LogInfo($"[RECOVERY] State sent to reconnected user {userId} in {matchCode}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"State recovery failed for user {userId}: {ex.Message}");
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
                logger.LogError($"Error in LeaveGame for user {userId}", ex);
            }
            finally
            {
                GameCallbackRegistry.Instance.UnregisterCallback(userId);
                logger.LogInfo($"User {userId} removed from callbacks/match {matchCode}");
            }
        }

        public async Task<DrawCardResultCode> DrawCard(string matchCode, int userId)
        {
            try
            {
                gameLogic.DrawCard(matchCode, userId);
                return DrawCardResultCode.DrawCard_Success;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning($"DrawCard logic denied: {ex.Message}");

                if (ex.Message.Contains("turn"))
                {
                    return DrawCardResultCode.DrawCard_NotYourTurn;
                }

                if (ex.Message.Contains("moves"))
                {
                    return DrawCardResultCode.DrawCard_AlreadyDrewThisTurn;
                }

                if (ex.Message.Contains("empty"))
                { 
                    return DrawCardResultCode.DrawCard_DrawPileEmpty; 
                }

                return DrawCardResultCode.DrawCard_InvalidParameter;
            }
            catch (Exception ex)
            {
                logger.LogError("DrawCard unexpected error", ex);
                return DrawCardResultCode.DrawCard_UnexpectedError;
            }
        }

        public async Task<PlayCardResultCode> PlayDinoHead(string matchCode, int userId, int cardId)
        {
            try
            {
                gameLogic.PlayDinoHead(matchCode, userId, cardId);
                return PlayCardResultCode.PlayCard_Success;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning($"PlayDinoHead logic denied: {ex.Message}");

                if (ex.Message.Contains("turn")) 
                { 
                    return PlayCardResultCode.PlayCard_NotYourTurn; 
                }

                if (ex.Message.Contains("moves"))
                {
                    return PlayCardResultCode.PlayCard_AlreadyPlayedTwoCards;
                }

                if (ex.Message.Contains("Card not found"))
                {
                    return PlayCardResultCode.PlayCard_CardNotInHand;
                }

                if (ex.Message.Contains("valid"))
                {
                    return PlayCardResultCode.PlayCard_InvalidDinoHead;
                }

                return PlayCardResultCode.PlayCard_UnexpectedError;
            }
            catch (Exception ex)
            {
                logger.LogError("PlayDinoHead unexpected error", ex);
                return PlayCardResultCode.PlayCard_UnexpectedError;
            }
        }

        public async Task<PlayCardResultCode> AttachBodyPartToDino(string matchCode, int userId, AttachBodyPartDTO attachmentData)
        {
            try
            {
                bool success = gameLogic.AttachBodyPart(matchCode, userId, attachmentData);

                if (success)
                {
                    return PlayCardResultCode.PlayCard_Success;
                }

                return PlayCardResultCode.PlayCard_InvalidDinoHead;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning($"AttachBodyPart logic denied: {ex.Message}");

                if (ex.Message.Contains("turn"))
                {
                    return PlayCardResultCode.PlayCard_NotYourTurn;
                }

                if (ex.Message.Contains("moves"))
                {
                    return PlayCardResultCode.PlayCard_AlreadyPlayedTwoCards;
                }

                if (ex.Message.Contains("attach"))
                {
                    return PlayCardResultCode.PlayCard_ArmyTypeMismatch;
                }

                return PlayCardResultCode.PlayCard_UnexpectedError;
            }
            catch (Exception ex)
            {
                logger.LogError("AttachBodyPart unexpected error", ex);
                return PlayCardResultCode.PlayCard_UnexpectedError;
            }
        }

        public async Task<DrawCardResultCode> TakeCardFromDiscardPile(string matchCode, int userId, int cardId)
        {
            try
            {
                gameLogic.TakeCardFromDiscardPile(matchCode, userId, cardId);
                return DrawCardResultCode.DrawCard_Success;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning($"TakeCardFromDiscard logic denied: {ex.Message}");

                if (ex.Message.Contains("turn"))
                {
                    return DrawCardResultCode.DrawCard_NotYourTurn;
                }

                if (ex.Message.Contains("moves"))
                {
                    return DrawCardResultCode.DrawCard_AlreadyDrewThisTurn;
                }

                if (ex.Message.Contains("not found"))
                {
                    return DrawCardResultCode.DrawCard_InvalidDrawPile;
                }

                return DrawCardResultCode.DrawCard_InvalidParameter;
            }
            catch (Exception ex)
            {
                logger.LogError("TakeCardFromDiscard unexpected error", ex);
                return DrawCardResultCode.DrawCard_UnexpectedError;
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
                logger.LogWarning($"Provoke logic denied: {ex.Message}");

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

        public async Task<EndTurnResultCode> EndTurn(string matchCode, int userId)
        {
            try
            {
                bool success = gameLogic.EndTurn(matchCode, userId);
                return success ? EndTurnResultCode.EndTurn_Success : EndTurnResultCode.EndTurn_NotYourTurn;
            }
            catch (System.Data.SqlClient.SqlException sqlEx)
            {
                logger.LogError("CRITICAL: DB Failed saving stats during EndTurn", sqlEx);
                return EndTurnResultCode.EndTurn_DatabaseError;
            }
            catch (System.Data.Entity.Core.EntityException entityEx)
            {
                logger.LogError("CRITICAL: EF Failed saving stats during EndTurn", entityEx);
                return EndTurnResultCode.EndTurn_DatabaseError;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning($"EndTurn logic denied: {ex.Message}");
                return EndTurnResultCode.EndTurn_NotYourTurn;
            }
            catch (Exception ex)
            {
                logger.LogError("EndTurn unexpected error", ex);
                return EndTurnResultCode.EndTurn_UnexpectedError;
            }
        }


        private CardDTO MapCardToDTO(ArchsVsDinosServer.BusinessLogic.GameManagement.Cards.CardInGame card)
        {
            if (card == null) return null;
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
                SandArmy = board.SandArmy.Select(id => ArchsVsDinosServer.BusinessLogic.GameManagement.Cards.CardInGame.FromDefinition(id)).Select(MapCardToDTO).ToList(),
                WaterArmy = board.WaterArmy.Select(id => ArchsVsDinosServer.BusinessLogic.GameManagement.Cards.CardInGame.FromDefinition(id)).Select(MapCardToDTO).ToList(),
                WindArmy = board.WindArmy.Select(id => ArchsVsDinosServer.BusinessLogic.GameManagement.Cards.CardInGame.FromDefinition(id)).Select(MapCardToDTO).ToList(),
            };
        }
    }
}
