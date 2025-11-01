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
        public bool Success { get; set; }

        [DataMember]
        public RegisterResultCode ResultCode { get; set; }

        public override bool Equals(object objectResponse)
        {
            if (objectResponse == null || GetType() != objectResponse.GetType())
                return false;
            var other = (RegisterResponse)objectResponse;
            return Success == other.Success;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Success.GetHashCode();
                return hash;
            }

        }

    }

}

