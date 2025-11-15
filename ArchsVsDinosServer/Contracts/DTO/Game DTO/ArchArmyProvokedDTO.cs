using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class ArchArmyProvokedDTO
    {
        [DataMember]
        public int MatchId { get; set; }

        [DataMember]
        public int ProvokerUserId { get; set; }

        [DataMember]
        public string ProvokerUsername { get; set; }

        [DataMember]
        public string ArmyType { get; set; }

        [DataMember]
        public BattleResultDTO BattleResult { get; set; }
    }
}
