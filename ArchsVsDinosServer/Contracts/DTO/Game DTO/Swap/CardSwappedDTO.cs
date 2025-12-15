using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO.Swap
{
    [DataContract]
    public class CardSwappedDTO
    {
        [DataMember]
        public string MatchCode { get; set; }

        [DataMember]
        public int InitiatorUserId { get; set; }

        [DataMember]
        public int TargetUserId { get; set; }

        [DataMember]
        public int CardInitiatorGaveId { get; set; }

        [DataMember]
        public int CardInitiatorReceivedId { get; set; }
    }
}
