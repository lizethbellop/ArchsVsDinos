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
        public int IdCard { get; set; }  

        [DataMember]
        public int Power { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public string Element { get; set; }

        [DataMember]
        public string BodyPart { get; set; }
    }
}
