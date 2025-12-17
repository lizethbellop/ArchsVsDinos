using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    [DataContract]
    public class ModerationResult
    {
        [DataMember]
        public bool CanSendMessage { get; set; }

        [DataMember]
        public bool ShouldBan { get; set; }

        [DataMember]
        public int CurrentStrikes { get; set; }

        [DataMember]
        public string Reason { get; set; }
    }

}
