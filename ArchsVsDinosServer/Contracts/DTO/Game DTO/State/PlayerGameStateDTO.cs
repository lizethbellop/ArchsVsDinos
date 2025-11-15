using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO.State
{
    [DataContract]
    public class PlayerGameStateDTO
    {
        [DataMember]
        public int UserId { get; set; }

        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public int HandCardCount { get; set; }

        [DataMember]
        public List<DinoDTO> Dinos { get; set; }

        [DataMember]
        public int CurrentPoints { get; set; }
    }
}
