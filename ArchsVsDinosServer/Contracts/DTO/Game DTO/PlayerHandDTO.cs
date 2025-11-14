using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class PlayerHandDTO
    {
        [DataMember]
        public int UserId { get; set; }

        [DataMember]
        public List<CardDTO> Cards { get; set; }
    }
}
