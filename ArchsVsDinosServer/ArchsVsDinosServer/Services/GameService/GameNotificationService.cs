using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces;
using Contracts.DTO;
using Contracts.DTO.Game_DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services.GameService
{
    public class GameNotificationService
    {
        private readonly ILoggerHelper logger;
        private readonly CardHelper cardHelper;

        public GameNotificationService(ILoggerHelper logger, CardHelper cardHelper)
        {
            this.logger = logger;
            this.cardHelper = cardHelper;
        }

        #region Game Lifecycle Notifications

        public void NotifyGameInitialized(GameSession session)
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

        public void NotifyGameStarted(GameSession session, PlayerSession firstPlayer, GameEndHandler endHandler)
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

        public void NotifyGameEnded(GameSession session, GameEndResult result)
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

        #endregion

        #region Turn Notifications

        public void NotifyTurnChanged(GameSession session, PlayerSession currentPlayer, GameEndHandler endHandler)
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

        #endregion

        #region Card Action Notifications

        public void NotifyCardDrawn(GameSession session, PlayerSession player, CardInGame card, int pileNumber)
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

        public void NotifyDinoPlayed(GameSession session, PlayerSession player, DinoInstance dino)
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

        public void NotifyBodyPartAttached(GameSession session, PlayerSession player, DinoInstance dino, CardInGame bodyCard)
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

        #endregion

        #region Battle Notifications

        public void NotifyBattleResolved(GameSession session, PlayerSession provoker, BattleResult battleResult)
        {
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

        #endregion

        #region Player Expulsion Notifications

        public void NotifyPlayerExpelled(GameSession session, PlayerSession expelledPlayer, string reason)
        {
            var dto = new PlayerExpelledDTO
            {
                MatchId = session.MatchId,
                ExpelledUserId = expelledPlayer.UserId,
                ExpelledUsername = expelledPlayer.Username,
                Reason = reason
            };

            NotifyAllPlayers(session, p => p.Callback?.OnPlayerExpelled(dto));
        }

        #endregion

        #region Helper Methods

        private void NotifyAllPlayers(GameSession session, Action<PlayerSession> notifyAction)
        {
            foreach (var player in session.Players)
            {
                try
                {
                    notifyAction(player);
                }
                catch (CommunicationObjectAbortedException)
                {
                    logger.LogWarning($"NotifyAllPlayers: Connection aborted for player {player.UserId}");
                }
                catch (CommunicationException)
                {
                    logger.LogWarning($"NotifyAllPlayers: Communication issue notifying player {player.UserId}");
                }
                catch (TimeoutException)
                {
                    logger.LogWarning($"NotifyAllPlayers: Timeout notifying player {player.UserId}");
                }
                catch (Exception)
                {
                    logger.LogInfo($"NotifyAllPlayers: Failed to notify player {player.UserId}");
                }
            }
        }

        #endregion
    }
}
