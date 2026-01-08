using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    [DataContract]
    public class BanResult
    {
        [DataMember]
        public bool CanSendMessage { get; set; }

        [DataMember]
        public bool ShouldBan { get; set; }

        [DataMember]
        public int CurrentStrikes { get; set; }

        [DataMember]
        public bool IsGuest { get; set; }
    }
}
