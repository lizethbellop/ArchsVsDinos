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
        public int IdPlayer { get; set; }
        [DataMember]
        public string Facebook { get; set; }
        [DataMember]
        public string Instagram { get; set; }
        [DataMember]
        public string X { get; set; }

        [DataMember]
        public string Tiktok { get; set; }
        [DataMember]
        public int TotalWins { get; set; }
        [DataMember]
        public int TotalLosses { get; set; }
        [DataMember]
        public int TotalPoints { get; set; }

        [DataMember]
        public string ProfilePicture { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = (PlayerDTO)obj;
            return IdPlayer == other.IdPlayer &&
                   Facebook == other.Facebook &&
                   Instagram == other.Instagram &&
                   X == other.X &&
                   Tiktok == other.Tiktok &&
                   TotalWins == other.TotalWins &&
                   TotalLosses == other.TotalLosses &&
                   TotalPoints == other.TotalPoints &&
                   ProfilePicture == other.ProfilePicture;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + IdPlayer.GetHashCode();
                hash = hash * 23 + (Facebook?.GetHashCode() ?? 0);
                hash = hash * 23 + (Instagram?.GetHashCode() ?? 0);
                hash = hash * 23 + (X?.GetHashCode() ?? 0);
                hash = hash * 23 + (Tiktok?.GetHashCode() ?? 0);
                hash = hash * 23 + TotalWins.GetHashCode();
                hash = hash * 23 + TotalLosses.GetHashCode();
                hash = hash * 23 + TotalPoints.GetHashCode();
                hash = hash * 23 + (ProfilePicture?.GetHashCode() ?? 0);
                return hash;
            }
        }

    }
}
