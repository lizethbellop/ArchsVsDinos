using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class CardDrawnDTO
    {
        [DataMember]
        public int MatchId { get; set; }

        [DataMember]
        public int PlayerUserId { get; set; }

        [DataMember]
        public string PlayerUsername { get; set; }

        [DataMember]
        public int DrawPileNumber { get; set; }

        [DataMember]
        public CardDTO Card { get; set; }
    }
}
