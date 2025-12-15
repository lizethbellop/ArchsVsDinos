using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Response
{
    [DataContract]
    public class MatchJoinResponse
    {
        [DataMember]
        public bool Success { get; set; }
        [DataMember]
        public JoinMatchResultCode ResultCode { get; set; }
        [DataMember]
        public string LobbyCode { get; set; }
    }
}
