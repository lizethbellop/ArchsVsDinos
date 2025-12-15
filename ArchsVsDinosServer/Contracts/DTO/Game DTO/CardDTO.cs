using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Contracts.DTO.Game_DTO.Enums;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class CardDTO
    {
        [DataMember]
        public int IdCard { get; set; }

        [DataMember]
        public int Power { get; set; }

        [DataMember]
        public ArmyType Element { get; set; }

        [DataMember]
        public DinoPartType PartType { get; set; }

        [DataMember]
        public bool HasTopJoint { get; set; }

        [DataMember]
        public bool HasBottomJoint { get; set; }

        [DataMember]
        public bool HasLeftJoint { get; set; }

        [DataMember]
        public bool HasRightJoint { get; set; }
    }
}
