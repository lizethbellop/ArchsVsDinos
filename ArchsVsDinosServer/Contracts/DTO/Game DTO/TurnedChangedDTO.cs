using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class TurnChangedDTO
    {
        [DataMember]
        public int MatchId { get; set; }

        [DataMember]
        public int CurrentPlayerUserId { get; set; }

        [DataMember]
        public string CurrentPlayerUsername { get; set; }

        [DataMember]
        public int TurnNumber { get; set; }
    }
}
