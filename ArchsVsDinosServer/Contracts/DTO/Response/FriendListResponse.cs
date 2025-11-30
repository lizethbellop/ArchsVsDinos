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
    public class FriendListResponse
    {
        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public FriendResultCode ResultCode { get; set; }

        [DataMember]
        public List<string> Friends { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            FriendListResponse other = (FriendListResponse)obj;

            bool friendsEqual = false;
            if (Friends == null && other.Friends == null)
                friendsEqual = true;
            else if (Friends != null && other.Friends != null)
                friendsEqual = Friends.SequenceEqual(other.Friends);

            return Success == other.Success &&
                   ResultCode == other.ResultCode &&
                   friendsEqual;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Success.GetHashCode();
                hash = hash * 23 + ResultCode.GetHashCode();
                hash = hash * 23 + (Friends?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
