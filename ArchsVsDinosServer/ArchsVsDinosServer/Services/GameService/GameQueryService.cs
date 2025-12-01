using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
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

                var playerHandCards = player.Hand.ToList();
                var playerHandData = new PlayerHandDTO
                {
                    UserId = userId,
                    Cards = CardConverter.ToDTOList(playerHandCards)
                };

                return playerHandData;
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
            var playersInfo = session.Players.Select(player => new PlayerInGameDTO
            {
                UserId = player.UserId,
                Username = player.Username,
                TurnOrder = player.TurnOrder
            }).ToList();

            var gameStateData = new GameStateDTO
            {
                MatchId = session.MatchId,
                IsStarted = session.IsStarted,
                CurrentTurnUserId = session.CurrentTurn,
                TurnNumber = session.TurnNumber,
                RemainingTime = endHandler.GetRemainingTime(session),
                Players = playersInfo,
                CentralBoard = BuildCentralBoardDTO(session),
                DrawPile1Count = session.GetDrawPileCount(0),
                DrawPile2Count = session.GetDrawPileCount(1),
                DrawPile3Count = session.GetDrawPileCount(2),
                DiscardPileCount = session.DiscardPile.Count
            };

            return gameStateData;
        }

        private CentralBoardDTO BuildCentralBoardDTO(GameSession session)
        {
            var centralBoardData = new CentralBoardDTO
            {
                LandArmyCount = session.CentralBoard.SandArmy.Count,
                SeaArmyCount = session.CentralBoard.WaterArmy.Count,
                SkyArmyCount = session.CentralBoard.WindArmy.Count,
                LandArmyPower = session.CentralBoard.GetArmyPower("sand"),
                SeaArmyPower = session.CentralBoard.GetArmyPower("water"),
                SkyArmyPower = session.CentralBoard.GetArmyPower("wind")
            };

            return centralBoardData;
        }
    }
}