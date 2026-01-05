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
    public class MatchCreationResponse
    {
        [DataMember]
        public bool Success { get; set; }
        [DataMember]
        public MatchCreationResultCode ResultCode { get; set; }
        [DataMember]
        public string LobbyCode { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = (MatchCreationResponse)obj;
            return Success == other.Success &&
                   ResultCode == other.ResultCode &&
                   LobbyCode == other.LobbyCode;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Success.GetHashCode();
                hash = hash * 23 + ResultCode.GetHashCode();
                hash = hash * 23 + (LobbyCode?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
