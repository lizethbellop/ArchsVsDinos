using Contracts.DTO.Game_DTO.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class GameStartedDTO
    {
        [DataMember]
        public int MatchId { get; set; }

        [DataMember]
        public int FirstPlayerUserId { get; set; }

        [DataMember]
        public string FirstPlayerUsername { get; set; }

        [DataMember]
        public int MyUserId { get; set; }

        [DataMember]
        public List<PlayerHandDTO> PlayersHands { get; set; }

        [DataMember]
        public CentralBoardDTO InitialBoard { get; set; }

        [DataMember]
        public int DrawPile1Count { get; set; }

        [DataMember]
        public int DrawPile2Count { get; set; }

        [DataMember]
        public int DrawPile3Count { get; set; }
        
        [DataMember]
        public DateTime StartTime { get; set; }
    }
}
