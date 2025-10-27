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
        public string username { get; set; }
        [DataMember]
        public string nickname { get; set; }
        [DataMember]
        public string profilePicture { get; set; }
        [DataMember]
        public bool isHost { get; set; }

    }
}
