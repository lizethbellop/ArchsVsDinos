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
    public class FriendRequestListResponse
    {
        [DataMember]
        public bool success { get; set; }

        [DataMember]
        public FriendRequestResultCode resultCode { get; set; }

        [DataMember]
        public List<string> requests { get; set; }
    }
}
