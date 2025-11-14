using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class ArchArmyDTO
    {
        [DataMember]
        public string ArmyType { get; set; }

        [DataMember]
        public List<CardDTO> ArchCards { get; set; }

        [DataMember]
        public int TotalPower { get; set; }
    }
}
