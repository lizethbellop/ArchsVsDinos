using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    [DataContract]
    public class LobbyInvitationDTO
    {
        [DataMember]
        public string LobbyCode { get; set; }

        [DataMember]
        public string SenderNickname { get; set; }

        [DataMember]
        public DateTime SentAt { get; set; }
    }
}
