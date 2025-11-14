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
        public int LandArmyCount { get; set; }

        [DataMember]
        public int SeaArmyCount { get; set; }

        [DataMember]
        public int SkyArmyCount { get; set; }

        [DataMember]
        public int LandArmyPower { get; set; }

        [DataMember]
        public int SeaArmyPower { get; set; }

        [DataMember]
        public int SkyArmyPower { get; set; }
    }
}
