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
    public class ArchArmyProvokedDTO
    {
        [DataMember]
        public string MatchCode { get; set; }

        [DataMember]
        public int ProvokerUserId { get; set; }

        [DataMember]
        public ArmyType ArmyType { get; set; }

        [DataMember]
        public BattleResultDTO BattleResult { get; set; }
    }
}
