using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Response
{
    [DataContract]
    public class FriendCheckResponse
    {
        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public FriendResultCode ResultCode { get; set; }

        [DataMember]
        public bool AreFriends { get; set; }
    }
}
