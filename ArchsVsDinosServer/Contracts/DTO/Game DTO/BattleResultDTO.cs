using Contracts.DTO.Game_DTO.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class BattleResultDTO
    {
        [DataMember]
        public string MatchCode { get; set; }

        [DataMember]
        public ArmyType ArmyType { get; set; }

        [DataMember]
        public int ArchPower { get; set; }

        [DataMember]
        public bool DinosWon { get; set; }

        [DataMember]
        public int? WinnerUserId { get; set; }

        [DataMember]
        public string WinnerUsername { get; set; }

        [DataMember]
        public int WinnerPower { get; set; }

        [DataMember]
        public int PointsAwarded { get; set; }

        [DataMember]
        public List<CardDTO> ArchCards { get; set; }

        [DataMember]
        public Dictionary<int, int> PlayerPowers { get; set; }
    }
}
