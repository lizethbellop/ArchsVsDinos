using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    [DataContract]
    public class LoginResponse
    {
        [DataMember]
        public bool success { get; set; }

        [DataMember]
        public string message { get; set; }
        [DataMember]
        public UserDTO userSession { get; set; }

        [DataMember]
        public PlayerDTO associatedPlayer { get; set; }

        [DataMember]
        public LoginResultCode resultCode { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var other = (LoginResponse)obj;
            return success == other.success &&
                   message == other.message &&
                   Equals(userSession, other.userSession) &&
                   Equals(associatedPlayer, other.associatedPlayer);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + success.GetHashCode();
                hash = hash * 23 + (message?.GetHashCode() ?? 0);
                hash = hash * 23 + (userSession?.GetHashCode() ?? 0);
                hash = hash * 23 + (associatedPlayer?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
