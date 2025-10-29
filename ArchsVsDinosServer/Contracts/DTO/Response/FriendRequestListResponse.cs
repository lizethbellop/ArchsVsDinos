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
        public bool Success { get; set; }

        [DataMember]
        public FriendRequestResultCode ResultCode { get; set; }

        [DataMember]
        public List<string> Requests { get; set; }
    }
}
