using ArchsVsDinosServer.BusinessLogic.Game_Manager.Cards;
using ArchsVsDinosServer.BusinessLogic.Game_Manager.Session;
using ArchsVsDinosServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.Game_Management
{
    public class BattleResolver
    {
        private readonly CardHelper cardHelper;

        public BattleResolver(ServiceDependencies dependencies)
        {
            cardHelper = new CardHelper(dependencies);
        }

        public BattleResult ResolveBattle(GameSession session, string armyType)
        {
            if (session == null || string.IsNullOrWhiteSpace(armyType))
            {
                return null;
            }

            var archArmy = session.CentralBoard.GetArmyByType(armyType);
            if (archArmy == null || archArmy.Count == 0)
            {
                return null;
            }

            int archPower = CalculateArchPower(archArmy);

            var playerDinos = GetAllPlayerDinosOfType(session, armyType);

            var playerPowers = new Dictionary<int, int>();
            foreach (var kvp in playerDinos)
            {
                var totalPower = kvp.Value.Sum(dino => dino.GetTotalPower());
                playerPowers[kvp.Key] = totalPower;
            }

            var maxPower = playerPowers.Any() ? playerPowers.Values.Max() : 0;
            var dinosWin = maxPower >= archPower; // Empate = Dinos ganan

            PlayerSession winner = null;
            if (dinosWin && maxPower > 0)
            {
                var winnerUserId = playerPowers.First(p => p.Value == maxPower).Key;
                winner = session.Players.FirstOrDefault(p => p.UserId == winnerUserId);
            }

            var result = new BattleResult
            {
                ArmyType = armyType,
                ArchPower = archPower,
                DinosWon = dinosWin,
                Winner = winner,
                WinnerPower = maxPower,
                ArchCardIds = new List<string>(archArmy),
                PlayerDinos = playerDinos
            };

            ApplyBattleConsequences(session, result);

            return result;
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

            foreach (var kvp in result.PlayerDinos)
            {
                var player = session.Players.FirstOrDefault(p => p.UserId == kvp.Key);
                if (player != null)
                {
                    DiscardPlayerDinos(session, player, kvp.Value);
                }
            }
        }

        private Dictionary<int, List<DinoInstance>> GetAllPlayerDinosOfType(GameSession session, string armyType)
        {
            var result = new Dictionary<int, List<DinoInstance>>();

            var dinoType = ArmyTypeHelper.ToDinoType(armyType);
            if (string.IsNullOrWhiteSpace(dinoType))
            {
                return result;
            }

            foreach (var player in session.Players)
            {
                var playerDinos = player.Dinos
                    .Where(d => d.ArmyType == dinoType)
                    .ToList();

                if (playerDinos.Any())
                {
                    result[player.UserId] = playerDinos;
                }
            }

            return result;
        }

        private int CalculateArchPower(List<string> archCardIds)
        {
            int totalPower = 0;

            foreach (var cardId in archCardIds)
            {
                var card = cardHelper.CreateCardInGame(cardId);
                if (card != null)
                {
                    totalPower += card.Power;
                }
            }

            return totalPower;
        }

        private int CalculateArchPoints(List<string> archCardIds)
        {
            return CalculateArchPower(archCardIds);
        }

        private void DiscardPlayerDinos(GameSession session, PlayerSession player, List<DinoInstance> dinos)
        {
            foreach (var dino in dinos)
            {
                if (dino.HeadCard != null)
                {
                    session.AddToDiscard(dino.HeadCard.IdCardGlobal);
                }

                foreach (var bodyPart in dino.BodyParts)
                {
                    session.AddToDiscard(bodyPart.IdCardGlobal);
                }

                player.RemoveDino(dino);
            }
        }
    }
}
