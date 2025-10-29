using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Contracts.DTO.Result_Codes;

namespace Contracts.DTO.Response
{
    [DataContract]
    public class FriendRequestResponse
    {
        [DataMember]
        public bool success { get; set; }

        [DataMember]
        public FriendRequestResultCode resultCode { get; set; }
    }
}
