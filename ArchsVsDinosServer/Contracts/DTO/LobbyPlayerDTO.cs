using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    public class LobbyPlayerDTO
    {

        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public string Nickname { get; set; }
        [DataMember]
        public string ProfilePicture { get; set; }
        [DataMember]
        public bool IsHost { get; set; }

    }
}
