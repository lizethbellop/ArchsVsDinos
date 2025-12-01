using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces;
using Contracts.DTO;
using Contracts.DTO.Game_DTO;
using ArchsVsDinosServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace ArchsVsDinosServer.Services.GameService
{
    public class GameNotificationService
    {
        private readonly ILoggerHelper logger;

        public GameNotificationService(ILoggerHelper logger)
        {
            this.logger = logger;
        }

        #region Game Lifecycle Notifications

        public void NotifyGameInitialized(GameSession session)
        {
            var playersInfo = session.Players.Select(player => new PlayerInGameDTO
            {
                UserId = player.UserId,
                Username = player.Username,
                TurnOrder = player.TurnOrder
            }).ToList();

            var gameInitializedData = new GameInitializedDTO
            {
                MatchId = session.MatchId,
                Players = playersInfo,
                RemainingCardsInDeck = 96
            };

            NotifyAllPlayers(session, player => player.Callback?.OnGameInitialized(gameInitializedData));
        }

        public void NotifyGameStarted(GameSession session, PlayerSession firstPlayer, GameEndHandler endHandler)
        {
            var playersHands = session.Players.Select(player => new PlayerHandDTO
            {
                UserId = player.UserId,
                Cards = CardConverter.ToDTOList(player.Hand.ToList())
            }).ToList();

            var gameStartedData = new GameStartedDTO
            {
                MatchId = session.MatchId,
                FirstPlayerUserId = firstPlayer.UserId,
                FirstPlayerUsername = firstPlayer.Username,
                PlayersHands = playersHands,
                DrawPile1Count = session.GetDrawPileCount(0),
                DrawPile2Count = session.GetDrawPileCount(1),
                DrawPile3Count = session.GetDrawPileCount(2),
                StartTime = session.StartTime ?? DateTime.UtcNow
            };

            NotifyAllPlayers(session, player => player.Callback?.OnGameStarted(gameStartedData));
        }

        public void NotifyGameEnded(GameSession session, GameEndResult result)
        {
            var finalScores = session.Players
                .OrderByDescending(player => player.Points)
                .Select((player, index) => new PlayerScoreDTO
                {
                    UserId = player.UserId,
                    Username = player.Username,
                    Points = player.Points,
                    Position = index + 1
                }).ToList();

            var gameEndedData = new GameEndedDTO
            {
                MatchId = session.MatchId,
                Reason = result.Reason,
                WinnerUserId = result.Winner?.UserId ?? 0,
                WinnerUsername = result.Winner?.Username ?? string.Empty,
                WinnerPoints = result.WinnerPoints,
                FinalScores = finalScores
            };

            NotifyAllPlayers(session, player => player.Callback?.OnGameEnded(gameEndedData));
        }

        #endregion

        #region Turn Notifications

        public void NotifyTurnChanged(GameSession session, PlayerSession currentPlayer, GameEndHandler endHandler)
        {
            var turnChangedData = new TurnChangedDTO
            {
                MatchId = session.MatchId,
                CurrentPlayerUserId = currentPlayer.UserId,
                CurrentPlayerUsername = currentPlayer.Username,
                TurnNumber = session.TurnNumber,
                RemainingTime = endHandler.GetRemainingTime(session)
            };

            NotifyAllPlayers(session, player => player.Callback?.OnTurnChanged(turnChangedData));
        }

        #endregion

        #region Card Action Notifications

        public void NotifyCardDrawn(GameSession session, PlayerSession player, CardInGame card, int pileNumber)
        {
            var cardDrawnData = new CardDrawnDTO
            {
                MatchId = session.MatchId,
                PlayerUserId = player.UserId,
                PlayerUsername = player.Username,
                DrawPileNumber = pileNumber,
                Card = CardConverter.ToDTO(card)
            };

            NotifyAllPlayers(session, playerToNotify => playerToNotify.Callback?.OnCardDrawn(cardDrawnData));
        }

        public void NotifyDinoPlayed(GameSession session, PlayerSession player, DinoInstance dino)
        {
            var dinoPlayedData = new DinoPlayedDTO
            {
                MatchId = session.MatchId,
                PlayerUserId = player.UserId,
                PlayerUsername = player.Username,
                DinoInstanceId = dino.DinoInstanceId,
                HeadCard = CardConverter.ToDTO(dino.HeadCard),
                ArmyType = dino.Element
            };

            NotifyAllPlayers(session, playerToNotify => playerToNotify.Callback?.OnDinoHeadPlayed(dinoPlayedData));
        }

        public void NotifyBodyPartAttached(GameSession session, PlayerSession player, DinoInstance dino, CardInGame bodyCard)
        {
            var bodyPartAttachedData = new BodyPartAttachedDTO
            {
                MatchId = session.MatchId,
                PlayerUserId = player.UserId,
                PlayerUsername = player.Username,
                DinoInstanceId = dino.DinoInstanceId,
                BodyCard = CardConverter.ToDTO(bodyCard),
                NewTotalPower = dino.GetTotalPower()
            };

            NotifyAllPlayers(session, playerToNotify => playerToNotify.Callback?.OnBodyPartAttached(bodyPartAttachedData));
        }

        #endregion

        #region Battle Notifications

        public void NotifyBattleResolved(GameSession session, PlayerSession provoker, BattleResult battleResult)
        {
            var archCardDTOs = battleResult.ArchCardIds
                .Select(cardId => CardInGame.FromDefinition(cardId))
                .Where(card => card != null)
                .Select(card => CardConverter.ToDTO(card))
                .Where(cardDTO => cardDTO != null)
                .ToList();

            var playerPowersDictionary = new Dictionary<int, int>();
            foreach (var playerDinosPair in battleResult.PlayerDinos)
            {
                var playerId = playerDinosPair.Key;
                var dinosList = playerDinosPair.Value;
                var totalPower = dinosList.Sum(dino => dino.GetTotalPower());
                playerPowersDictionary[playerId] = totalPower;
            }

            var battleResultData = new BattleResultDTO
            {
                MatchId = session.MatchId,
                ArmyType = battleResult.ArmyType,
                ArchPower = battleResult.ArchPower,
                DinosWon = battleResult.DinosWon,
                WinnerUserId = battleResult.Winner?.UserId,
                WinnerUsername = battleResult.Winner?.Username,
                WinnerPower = battleResult.WinnerPower,
                PointsAwarded = battleResult.DinosWon ? battleResult.ArchPower : 0,
                ArchCards = archCardDTOs,
                PlayerPowers = playerPowersDictionary
            };

            var archArmyProvokedData = new ArchArmyProvokedDTO
            {
                MatchId = session.MatchId,
                ProvokerUserId = provoker.UserId,
                ProvokerUsername = provoker.Username,
                ArmyType = battleResult.ArmyType,
                BattleResult = battleResultData
            };

            NotifyAllPlayers(session, player => player.Callback?.OnArchArmyProvoked(archArmyProvokedData));
            NotifyAllPlayers(session, player => player.Callback?.OnBattleResolved(battleResultData));
        }

        #endregion

        #region Player Expulsion Notifications

        public void NotifyPlayerExpelled(GameSession session, PlayerSession expelledPlayer, string reason)
        {
            var playerExpelledData = new PlayerExpelledDTO
            {
                MatchId = session.MatchId,
                ExpelledUserId = expelledPlayer.UserId,
                ExpelledUsername = expelledPlayer.Username,
                Reason = reason
            };

            NotifyAllPlayers(session, player => player.Callback?.OnPlayerExpelled(playerExpelledData));
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