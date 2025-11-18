using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Statistics
{
    [DataContract]
    public class MatchHistoryDTO
    {
        [DataMember]
        public int MatchId { get; set; }

        [DataMember]
        public DateTime MatchDate { get; set; }

        [DataMember]
        public int Points { get; set; }

        [DataMember]
        public bool Won { get; set; }

        [DataMember]
        public int Position { get; set; }

        [DataMember]
        public int TotalPlayers { get; set; }
    }
}
