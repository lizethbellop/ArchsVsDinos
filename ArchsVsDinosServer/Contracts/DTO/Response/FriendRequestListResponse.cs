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

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            FriendRequestListResponse other = (FriendRequestListResponse)obj;

            if (Success != other.Success || ResultCode != other.ResultCode)
                return false;

            if (Requests == null && other.Requests == null)
                return true;

            if (Requests == null || other.Requests == null)
                return false;

            if (Requests.Count != other.Requests.Count)
                return false;

            for (int i = 0; i < Requests.Count; i++)
            {
                if (Requests[i] != other.Requests[i])
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Success.GetHashCode();
                hash = hash * 23 + ResultCode.GetHashCode();

                if (Requests != null)
                {
                    foreach (var request in Requests)
                    {
                        if (request != null)
                            hash = hash * 23 + request.GetHashCode();
                    }
                }

                return hash;
            }
        }
    }
}
