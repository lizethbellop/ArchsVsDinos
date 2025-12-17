using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    [DataContract]
    public class ModerationRequestDTO
    {
        [DataMember]
        public int UserId { get; set; }

        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public ChatContext Context { get; set; }

        [DataMember]
        public string MatchCode { get; set; }
    }

}
