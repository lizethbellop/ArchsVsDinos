using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class CardDTO
    {
        [DataMember]
        public string IdCardGlobal { get; set; }  

        [DataMember]
        public int? IdCardBody { get; set; } 

        [DataMember]
        public int? IdCardCharacter { get; set; } 

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public string ArmyType { get; set; }

        [DataMember]
        public int Power { get; set; }

        [DataMember]
        public string ImagePath { get; set; }
    }
}
