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
        public int Points { get; set; }
        [DataMember]
        public bool IsWinner { get; set; }
        [DataMember]
        public int ArchaeologistsEliminated { get; set; }
        [DataMember]
        public int SupremeBossesEliminated { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            PlayerMatchStatsDTO other = (PlayerMatchStatsDTO)obj;

            return UserId == other.UserId &&
                   Username == other.Username &&
                   Position == other.Position &&
                   Points == other.Points &&
                   IsWinner == other.IsWinner &&
                   ArchaeologistsEliminated == other.ArchaeologistsEliminated &&
                   SupremeBossesEliminated == other.SupremeBossesEliminated;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + UserId.GetHashCode();
            hash = hash * 23 + (Username?.GetHashCode() ?? 0);
            hash = hash * 23 + Position.GetHashCode();
            hash = hash * 23 + Points.GetHashCode();
            hash = hash * 23 + IsWinner.GetHashCode();
            hash = hash * 23 + ArchaeologistsEliminated.GetHashCode();
            hash = hash * 23 + SupremeBossesEliminated.GetHashCode();
            return hash;
        }
    }
}
