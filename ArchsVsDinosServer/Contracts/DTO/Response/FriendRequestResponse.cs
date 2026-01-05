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
        public bool Success { get; set; }
        [DataMember]
        public FriendRequestResultCode ResultCode { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            FriendRequestResponse other = (FriendRequestResponse)obj;
            return Success == other.Success && ResultCode == other.ResultCode;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Success.GetHashCode();
                hash = hash * 23 + ResultCode.GetHashCode();
                return hash;
            }
        }
    }
}
