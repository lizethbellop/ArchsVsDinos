using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Statistics
{
    [DataContract]
    public class PlayerMatchStatsDTO
    {
        [DataMember]
        public int UserId { get; set; }

        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public int Position { get; set; }

        [DataMember]
        public int ArchaeologistsEliminated { get; set; }

        [DataMember]
        public int SupremeBossesEliminated { get; set; }

        [DataMember]
        public int Points { get; set; }

        [DataMember]
        public bool IsWinner { get; set; }
    }
}
