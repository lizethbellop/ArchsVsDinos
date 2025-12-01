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

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            LeaderboardEntryDTO other = (LeaderboardEntryDTO)obj;

            return Position == other.Position &&
                   UserId == other.UserId &&
                   Username == other.Username &&
                   TotalPoints == other.TotalPoints &&
                   TotalWins == other.TotalWins;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Position.GetHashCode();
            hash = hash * 23 + UserId.GetHashCode();
            hash = hash * 23 + (Username?.GetHashCode() ?? 0);
            hash = hash * 23 + TotalPoints.GetHashCode();
            hash = hash * 23 + TotalWins.GetHashCode();
            return hash;
        }
    }
}
