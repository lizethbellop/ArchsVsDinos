using ArchsVsDinosServer.BusinessLogic;
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

        public void SwapCardWithPlayer(string matchCode, int initiatorUserId, ExchangeCardDTO request)
        {
            ExecuteSafe(() =>
            {
                gameLogic.ExchangeCard(matchCode, initiatorUserId, request);
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
}
