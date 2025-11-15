using ArchsVsDinosServer.BusinessLogic.Game_Manager.Cards;
using ArchsVsDinosServer.BusinessLogic.Game_Manager.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.Game_Management
{
    public class BattleResult
    {
        public string ArmyType { get; set; }
        public int ArchPower { get; set; }
        public bool DinosWon { get; set; }
        public PlayerSession Winner { get; set; }
        public int WinnerPower { get; set; }
        public List<string> ArchCardIds { get; set; }
        public Dictionary<int, List<DinoInstance>> PlayerDinos { get; set; }
    }
}
