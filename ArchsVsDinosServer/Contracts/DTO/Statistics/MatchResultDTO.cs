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
        public string MatchId { get; set; }
        [DataMember]
        public DateTime MatchDate { get; set; }
        [DataMember]
        public int WinnerUserId { get; set; }
        [DataMember]
        public List<PlayerMatchResult> PlayerResults { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            MatchResultDTO other = (MatchResultDTO)obj;

            bool playerResultsEqual = true;
            if (PlayerResults == null && other.PlayerResults == null)
            {
                playerResultsEqual = true;
            }
            else if (PlayerResults == null || other.PlayerResults == null)
            {
                playerResultsEqual = false;
            }
            else if (PlayerResults.Count != other.PlayerResults.Count)
            {
                playerResultsEqual = false;
            }
            else
            {
                for (int i = 0; i < PlayerResults.Count; i++)
                {
                    if (!PlayerResults[i].Equals(other.PlayerResults[i]))
                    {
                        playerResultsEqual = false;
                        break;
                    }
                }
            }

            return MatchId == other.MatchId &&
                   MatchDate == other.MatchDate &&
                   WinnerUserId == other.WinnerUserId &&
                   playerResultsEqual;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + MatchId.GetHashCode();
            hash = hash * 23 + MatchDate.GetHashCode();
            hash = hash * 23 + WinnerUserId.GetHashCode();

            if (PlayerResults != null)
            {
                foreach (var result in PlayerResults)
                {
                    hash = hash * 23 + result.GetHashCode();
                }
            }

            return hash;
        }
    }
}
