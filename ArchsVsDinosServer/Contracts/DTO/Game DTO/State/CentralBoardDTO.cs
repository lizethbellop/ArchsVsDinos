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
        public List<CardDTO> SandArmy { get; set; }

        [DataMember]
        public List<CardDTO> WaterArmy { get; set; }

        [DataMember]
        public List<CardDTO> WindArmy { get; set; }

        [DataMember]
        public int SandArmyCount { get; set; }

        [DataMember]
        public int WaterArmyCount { get; set; }

        [DataMember]
        public int WindArmyCount { get; set; }

        [DataMember]
        public int SandArmyPower { get; set; }

        [DataMember]
        public int WaterArmyPower { get; set; }

        [DataMember]
        public int WindArmyPower { get; set; }
    }
}
