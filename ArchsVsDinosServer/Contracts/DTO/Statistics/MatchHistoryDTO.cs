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

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            MatchHistoryDTO other = (MatchHistoryDTO)obj;
            return MatchId == other.MatchId &&
                   MatchDate == other.MatchDate &&
                   Points == other.Points &&
                   Won == other.Won &&
                   Position == other.Position &&
                   TotalPlayers == other.TotalPlayers;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + MatchId.GetHashCode();
                hash = hash * 23 + MatchDate.GetHashCode();
                hash = hash * 23 + Points.GetHashCode();
                hash = hash * 23 + Won.GetHashCode();
                hash = hash * 23 + Position.GetHashCode();
                hash = hash * 23 + TotalPlayers.GetHashCode();
                return hash;
            }
        }
    }
}
