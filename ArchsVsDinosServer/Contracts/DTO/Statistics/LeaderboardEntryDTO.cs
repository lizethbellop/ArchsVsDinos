using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Statistics
{
    [DataContract]
    public class LeaderboardEntryDTO
    {
        [DataMember]
        public int Position { get; set; }

        [DataMember]
        public int UserId { get; set; }

        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public int TotalPoints { get; set; }

        [DataMember]
        public int TotalWins { get; set; }
    }
}
