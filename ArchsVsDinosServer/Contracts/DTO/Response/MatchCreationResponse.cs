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
    }
}
