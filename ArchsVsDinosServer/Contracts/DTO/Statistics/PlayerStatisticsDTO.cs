using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Statistics
{
    [DataContract]
    public class PlayerStatisticsDTO
    {
        [DataMember]
        public int UserId { get; set; }

        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public int TotalWins { get; set; }

        [DataMember]
        public int TotalLosses { get; set; }

        [DataMember]
        public int TotalMatches { get; set; }

        [DataMember]
        public int TotalPoints { get; set; }

        [DataMember]
        public double WinRate { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            PlayerStatisticsDTO other = (PlayerStatisticsDTO)obj;
            return UserId == other.UserId &&
                   Username == other.Username &&
                   TotalWins == other.TotalWins &&
                   TotalLosses == other.TotalLosses &&
                   TotalMatches == other.TotalMatches &&
                   TotalPoints == other.TotalPoints &&
                   WinRate == other.WinRate;
        }

        public override int GetHashCode()
        {
            unchecked 
            {
                int hash = 17;
                hash = hash * 23 + UserId.GetHashCode();
                hash = hash * 23 + (Username != null ? Username.GetHashCode() : 0);
                hash = hash * 23 + TotalWins.GetHashCode();
                hash = hash * 23 + TotalLosses.GetHashCode();
                hash = hash * 23 + TotalMatches.GetHashCode();
                hash = hash * 23 + TotalPoints.GetHashCode();
                hash = hash * 23 + WinRate.GetHashCode();
                return hash;
            }
        }
    }
}
