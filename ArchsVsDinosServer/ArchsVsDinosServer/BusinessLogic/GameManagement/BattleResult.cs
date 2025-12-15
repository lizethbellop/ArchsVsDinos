using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using Contracts.DTO.Game_DTO.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement
{
    public class BattleResult
    {
        public ArmyType ArmyType { get; set; }

        public int ArchPower { get; set; }
        public bool DinosWon { get; set; }

        public PlayerSession Winner { get; set; }

        public int WinnerPower { get; set; }

        public List<int> ArchCardIds { get; set; }

        public Dictionary<int, List<DinoInstance>> PlayerDinos { get; set; }
    }
}
