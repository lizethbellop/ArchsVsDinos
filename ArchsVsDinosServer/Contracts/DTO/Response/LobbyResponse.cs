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
    public class LobbyInviteResponse
    {
        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public LobbyResultCode ResultCode { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var other = (LobbyInviteResponse)obj;
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
