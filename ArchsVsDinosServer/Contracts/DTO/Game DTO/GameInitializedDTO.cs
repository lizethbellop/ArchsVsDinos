using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class GameInitializedDTO
    {
        [DataMember]
        public string MatchCode { get; set; }

        [DataMember]
        public List<PlayerInGameDTO> Players { get; set; }

        [DataMember] 
        public int RemainingCardsInDeck { get; set; }
    }
}
