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
        public bool Success { get; set; }

        [DataMember]
        public UserDTO UserSession { get; set; }

        [DataMember]
        public PlayerDTO AssociatedPlayer { get; set; }

        [DataMember]
        public LoginResultCode ResultCode { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var other = (LoginResponse)obj;
            return Success == other.Success &&
                   Equals(UserSession, other.UserSession) &&
                   Equals(AssociatedPlayer, other.AssociatedPlayer);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Success.GetHashCode();
                hash = hash * 23 + (UserSession?.GetHashCode() ?? 0);
                hash = hash * 23 + (AssociatedPlayer?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
