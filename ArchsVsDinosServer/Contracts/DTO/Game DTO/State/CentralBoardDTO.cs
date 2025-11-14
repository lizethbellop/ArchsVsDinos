using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO.State
{
    [DataContract]
    public class CentralBoardDTO
    {
        [DataMember]
        public ArchArmyDTO LandArmy { get; set; }

        [DataMember]
        public ArchArmyDTO SeaArmy { get; set; }

        [DataMember]
        public ArchArmyDTO SkyArmy { get; set; }
    }
}
