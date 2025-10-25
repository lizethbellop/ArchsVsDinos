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
    public class RegisterResponse
    {
        [DataMember]
        public bool success { get; set; }

        [DataMember]
        public RegisterResultCode resultCode { get; set; }

        public override bool Equals(object objectResponse)
        {
            if (objectResponse == null || GetType() != objectResponse.GetType())
                return false;
            var other = (RegisterResponse)objectResponse;
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

