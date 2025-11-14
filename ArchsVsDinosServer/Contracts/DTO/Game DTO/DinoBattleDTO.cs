using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class DinoBattleDTO
    {
        [DataMember]
        public int PlayerUserId { get; set; }

        [DataMember]
        public string PlayerUsername { get; set; }

        [DataMember]
        public int DinoInstanceId { get; set; }

        [DataMember]
        public List<CardDTO> DinoCards { get; set; }

        [DataMember]
        public int TotalPower { get; set; }
    }
}
