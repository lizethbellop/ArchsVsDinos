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
    public class UpdateResponse
    {
        [DataMember]
        public bool success { get; set; }

        [DataMember]
        public UpdateResultCode resultCode { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var other = (UpdateResponse)obj;
            return success == other.success;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + success.GetHashCode();
                return hash;
            }
        }
    }
}
