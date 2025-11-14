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
        public int MatchId { get; set; }

        [DataMember]
        public string ArmyType { get; set; }

        [DataMember]
        public int ArchArmyPower { get; set; }

        [DataMember]
        public List<DinoBattleDTO> DinosInBattle { get; set; }

        [DataMember]
        public int WinnerUserId { get; set; }

        [DataMember]
        public string WinnerUsername { get; set; }

        [DataMember]
        public int PointsAwarded { get; set; }

        [DataMember]
        public bool ArchsWon { get; set; }
    }
}
