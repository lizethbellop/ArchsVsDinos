using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Statistics
{
    [DataContract]
    public class MatchResultDTO
    {
        [DataMember]
        public int MatchId { get; set; }

        [DataMember]
        public DateTime MatchDate { get; set; }

        [DataMember]
        public int WinnerUserId { get; set; }

        [DataMember]
        public List<PlayerMatchResult> PlayerResults { get; set; }
    }
}
