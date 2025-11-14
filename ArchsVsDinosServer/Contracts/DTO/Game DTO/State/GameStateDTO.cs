using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO.State
{
    [DataContract]
    public class GameStateDTO
    {
        [DataMember]
        public int MatchId { get; set; }

        [DataMember]
        public int CurrentPlayerUserId { get; set; }

        [DataMember]
        public int TurnNumber { get; set; }

        [DataMember]
        public CentralBoardDTO CentralBoard { get; set; }

        [DataMember]
        public List<PlayerGameStateDTO> PlayersState { get; set; }

        [DataMember]
        public int DrawPile1Count { get; set; }

        [DataMember]
        public int DrawPile2Count { get; set; }

        [DataMember]
        public int DrawPile3Count { get; set; }

        [DataMember]
        public int DiscardPileCount { get; set; }

        [DataMember]
        public bool IsGameEnded { get; set; }
    }
}
