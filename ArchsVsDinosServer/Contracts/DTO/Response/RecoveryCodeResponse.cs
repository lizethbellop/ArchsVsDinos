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
    public class RecoveryCodeResponse
    {
        [DataMember]
        public PasswordRecoveryResult Result { get; set; }

        [DataMember]
        public double RemainingSeconds { get; set; } 
    }
}
