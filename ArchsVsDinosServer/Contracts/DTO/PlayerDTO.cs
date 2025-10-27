using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    public class PlayerDTO
    {
        [DataMember]
        public int idPlayer { get; set; }
        [DataMember]
        public string facebook { get; set; }
        [DataMember]
        public string instagram { get; set; }
        [DataMember]
        public string x { get; set; }

        [DataMember]
        public string tiktok { get; set; }
        [DataMember]
        public int totalWins { get; set; }
        [DataMember]
        public int totalLosses { get; set; }
        [DataMember]
        public int totalPoints { get; set; }

        [DataMember]
        public string profilePicture { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = (PlayerDTO)obj;
            return idPlayer == other.idPlayer &&
                   facebook == other.facebook &&
                   instagram == other.instagram &&
                   x == other.x &&
                   tiktok == other.tiktok &&
                   totalWins == other.totalWins &&
                   totalLosses == other.totalLosses &&
                   totalPoints == other.totalPoints &&
                   profilePicture == other.profilePicture;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + idPlayer.GetHashCode();
                hash = hash * 23 + (facebook?.GetHashCode() ?? 0);
                hash = hash * 23 + (instagram?.GetHashCode() ?? 0);
                hash = hash * 23 + (x?.GetHashCode() ?? 0);
                hash = hash * 23 + (tiktok?.GetHashCode() ?? 0);
                hash = hash * 23 + totalWins.GetHashCode();
                hash = hash * 23 + totalLosses.GetHashCode();
                hash = hash * 23 + totalPoints.GetHashCode();
                hash = hash * 23 + (profilePicture?.GetHashCode() ?? 0);
                return hash;
            }
        }

    }
}
