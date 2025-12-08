using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
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
            // Ya no necesitamos CardHelper
        }

        public BattleResult ResolveBattle(GameSession session, string armyType)
        {
            if (session == null || string.IsNullOrWhiteSpace(armyType))
            {
                return null;
            }

            var normalizedArmyType = ArmyTypeHelper.NormalizeElement(armyType);
            var archArmy = session.CentralBoard.GetArmyByType(normalizedArmyType);

            if (archArmy == null || archArmy.Count == 0)
            {
                return null;
            }

            int archPower = CalculateArchPower(archArmy);

            var playerDinos = GetAllPlayerDinosOfType(session, normalizedArmyType);

            var playerPowers = new Dictionary<int, int>();
            foreach (var playerDinosPair in playerDinos)
            {
                var playerId = playerDinosPair.Key;
                var dinosList = playerDinosPair.Value;
                var totalPower = dinosList.Sum(dino => dino.TotalPower);
                playerPowers[playerId] = totalPower;
            }

            var maxPower = playerPowers.Any() ? playerPowers.Values.Max() : 0;
            var dinosWin = maxPower >= archPower; 

            PlayerSession winner = null;
            if (dinosWin && maxPower > 0)
            {
                var winnerUserId = playerPowers.First(powerPair => powerPair.Value == maxPower).Key;
                winner = session.Players.FirstOrDefault(player => player.UserId == winnerUserId);
            }

            var battleResult = new BattleResult
            {
                ArmyType = normalizedArmyType,
                ArchPower = archPower,
                DinosWon = dinosWin,
                Winner = winner,
                WinnerPower = maxPower,
                ArchCardIds = new List<int>(archArmy),  
                PlayerDinos = playerDinos
            };

            ApplyBattleConsequences(session, battleResult);

            return battleResult;
        }

        private void ApplyBattleConsequences(GameSession session, BattleResult result)
        {
            if (session == null || result == null)
            {
                return;
            }

            if (result.DinosWon && result.Winner != null)
            {
                var archPoints = CalculateArchPoints(result.ArchCardIds);
                result.Winner.Points += archPoints;
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

        private Dictionary<int, List<DinoInstance>> GetAllPlayerDinosOfType(GameSession session, string element)
        {
            var result = new Dictionary<int, List<DinoInstance>>();
            var normalizedElement = ArmyTypeHelper.NormalizeElement(element);

            foreach (var player in session.Players)
            {
                var playerDinos = player.Dinos
                    .Where(dino => ArmyTypeHelper.NormalizeElement(dino.Element) == normalizedElement)
                    .ToList();

                if (playerDinos.Any())
                {
                    result[player.UserId] = playerDinos;
                }
            }

            return result;
        }

        private int CalculateArchPower(List<int> archCardIds)
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

            return totalPower;
        }

        private int CalculateArchPoints(List<int> archCardIds)
        {
            return CalculateArchPower(archCardIds);
        }

        private void DiscardPlayerDinos(GameSession session, PlayerSession player, List<DinoInstance> dinos)
        {
            foreach (var dino in dinos)
            {
                if (dino.HeadCard != null)
                {
                    session.AddToDiscard(dino.HeadCard.IdCard);  
                }

                foreach (var bodyPart in dino.BodyParts)
                {
                    session.AddToDiscard(bodyPart.IdCard);  
                }

                player.RemoveDino(dino);
            }
        }
    }
}