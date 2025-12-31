using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using Contracts.DTO.Game_DTO.Enums;
using ArchsVsDinosServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement
{
    public class BattleResolver
    {
        public BattleResolver(ServiceDependencies dependencies)
        {
        }

        public BattleResult ResolveBattle(GameSession session, ArmyType armyType)
        {
            if (session == null || armyType == ArmyType.None)
            {
                return null;
            }

            var archArmyCardIds = session.CentralBoard.GetArmyByType(armyType);

            if (archArmyCardIds == null || archArmyCardIds.Count == 0)
            {
                return null;
            }

            int archPower = CalculateArchPower(session.CentralBoard, archArmyCardIds, armyType);

            var playerDinos = GetAllPlayerDinosOfType(session, armyType);
            var playerPowers = new Dictionary<int, int>();

            foreach (var playerDinosPair in playerDinos)
            {
                var playerId = playerDinosPair.Key;
                var dinosList = playerDinosPair.Value;
                var totalPower = dinosList.Sum(dino => dino.TotalPower);
                playerPowers[playerId] = totalPower;
            }

            var maxPlayerPower = playerPowers.Any() ? playerPowers.Values.Max() : 0;

            bool dinosWin = maxPlayerPower > 0 && maxPlayerPower >= archPower;

            PlayerSession winner = null;

            if (dinosWin)
            {
                var tiedWinners = playerPowers.Where(p => p.Value == maxPlayerPower)
                                              .Select(p => p.Key)
                                              .ToList();

                if (tiedWinners.Count > 0)
                {
                    if (tiedWinners.Contains(session.CurrentTurn))
                    {
                        winner = session.Players.FirstOrDefault(p => p.UserId == session.CurrentTurn);
                    }
                    else
                    {
                        winner = session.Players.FirstOrDefault(p => p.UserId == tiedWinners.First());
                    }
                }
            }

            var battleResult = new BattleResult
            {
                ArmyType = armyType,
                ArchPower = archPower,
                DinosWon = dinosWin,
                Winner = winner,
                WinnerPower = maxPlayerPower,
                ArchCardIds = new List<int>(archArmyCardIds),
                PlayerDinos = playerDinos
            };

            ApplyBattleConsequences(session, battleResult);

            return battleResult;
        }

        private void ApplyBattleConsequences(GameSession session, BattleResult result)
        {
            if (session == null || result == null) return;

            if (result.DinosWon && result.Winner != null)
            {
                var pointsEarned = CalculateArchPoints(session.CentralBoard, result.ArchCardIds, result.ArmyType);
                result.Winner.Points += pointsEarned;
            }

            session.AddToDiscard(result.ArchCardIds);
            session.CentralBoard.ClearArmy(result.ArmyType);

            foreach (var playerDinosPair in result.PlayerDinos)
            {
                var playerId = playerDinosPair.Key;
                var dinosList = playerDinosPair.Value;
                var player = session.Players.FirstOrDefault(p => p.UserId == playerId);

                if (player != null)
                {
                    DiscardPlayerDinos(session, player, dinosList);
                }
            }
        }

        private Dictionary<int, List<DinoInstance>> GetAllPlayerDinosOfType(GameSession session, ArmyType element)
        {
            var result = new Dictionary<int, List<DinoInstance>>();

            foreach (var player in session.Players)
            {
                var playerDinos = player.Dinos
                    .Where(dino => dino.Element == element)
                    .ToList();

                if (playerDinos.Any())
                {
                    result[player.UserId] = playerDinos;
                }
            }
            return result;
        }

        private int CalculateArchPower(CentralBoard board, List<int> archCardIds, ArmyType armyType)
        {
            int totalPower = 0;

            foreach (var cardId in archCardIds)
            {
                var card = CardInGame.FromDefinition(cardId);
                if (card != null)
                {
                    totalPower += card.Power;
                }
            }

            var bossCard = board.SupremeBossCard;
            if (bossCard != null && bossCard.Element == armyType)
            {
                totalPower += bossCard.Power;
            }

            return totalPower;
        }

        private int CalculateArchPoints(CentralBoard board, List<int> archCardIds, ArmyType armyType)
        {
            int points = archCardIds.Count;

            var bossCard = board.SupremeBossCard;
            if (bossCard != null && bossCard.Element == armyType)
            {
                points += 3;
            }

            return points;
        }

        private void DiscardPlayerDinos(GameSession session, PlayerSession player, List<DinoInstance> dinos)
        {
            foreach (var dino in dinos)
            {
                var allDinoCards = dino.GetAllCards();

                foreach (var card in allDinoCards)
                {
                    session.AddToDiscard(card.IdCard);
                }

                player.RemoveDino(dino);
            }
        }
    }
}