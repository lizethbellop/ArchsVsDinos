using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    [DataContract]
    public class MatchSettings
    {
        [DataMember]
        public string HostNickname { get; set; }

        [DataMember]
        public string HostUsername { get; set; }

        [DataMember]
        public int MaxPlayers { get; set; }

        [DataMember]
        public int HostUserId { get; set; }

        [DataMember]
        public string HostProfilePicture { get; set; }

    }
}
