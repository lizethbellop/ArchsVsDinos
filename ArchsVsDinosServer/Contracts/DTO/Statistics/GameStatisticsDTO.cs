using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Statistics
{
    [DataContract]
    public class GameStatisticsDTO
    {
        [DataMember]
        public string MatchCode { get; set; }
        [DataMember]
        public DateTime MatchDate { get; set; }
        [DataMember]
        public PlayerMatchStatsDTO[] PlayerStats { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            GameStatisticsDTO other = (GameStatisticsDTO)obj;

            bool playerStatsEqual = true;
            if (PlayerStats == null && other.PlayerStats == null)
            {
                playerStatsEqual = true;
            }
            else if (PlayerStats == null || other.PlayerStats == null)
            {
                playerStatsEqual = false;
            }
            else if (PlayerStats.Length != other.PlayerStats.Length)
            {
                playerStatsEqual = false;
            }
            else
            {
                for (int i = 0; i < PlayerStats.Length; i++)
                {
                    if (!PlayerStats[i].Equals(other.PlayerStats[i]))
                    {
                        playerStatsEqual = false;
                        break;
                    }
                }
            }

            return MatchCode == other.MatchCode &&
                   MatchDate == other.MatchDate &&
                   playerStatsEqual;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (MatchCode?.GetHashCode() ?? 0);
            hash = hash * 23 + MatchDate.GetHashCode();

            if (PlayerStats != null)
            {
                foreach (var stat in PlayerStats)
                {
                    hash = hash * 23 + stat.GetHashCode();
                }
            }

            return hash;
        }
    }
}
