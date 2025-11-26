using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts.DTO.Game_DTO;
using Contracts.DTO.Game_DTO.State;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services.GameService
{
    public class GameQueryService
    {
        private readonly GameSessionManager sessionManager;
        private readonly GameEndHandler endHandler;
        private readonly ServiceDependencies dependencies;
        private readonly ILoggerHelper logger;

        public GameQueryService(
            GameSessionManager sessionManager,
            GameEndHandler endHandler,
            ServiceDependencies dependencies,
            ILoggerHelper logger)
        {
            this.sessionManager = sessionManager;
            this.endHandler = endHandler;
            this.dependencies = dependencies;
            this.logger = logger;
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
            catch (CommunicationException)
            {
                logger.LogWarning("GetGameState: Communication issue");
                return null;
            }
            catch (TimeoutException)
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
            catch (CommunicationException)
            {
                logger.LogWarning("GetPlayerHand: Communication issue");
                return null;
            }
            catch (TimeoutException)
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
            catch (CommunicationException)
            {
                logger.LogWarning("GetCentralBoard: Communication issue");
                return null;
            }
            catch (TimeoutException)
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
            var cardHelper = new CardHelper(dependencies);
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
    }
}
