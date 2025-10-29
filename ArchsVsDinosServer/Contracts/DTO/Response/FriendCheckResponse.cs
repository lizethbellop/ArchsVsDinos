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
        public bool success { get; set; }

        [DataMember]
        public FriendResultCode resultCode { get; set; }

        [DataMember]
        public bool areFriends { get; set; }
    }
}
