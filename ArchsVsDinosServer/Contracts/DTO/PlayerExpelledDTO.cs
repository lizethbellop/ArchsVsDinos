using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    [DataContract]
    public class PlayerExpelledDTO
    {
        [DataMember]
        public int MatchId { get; set; }

        [DataMember]
        public int ExpelledUserId { get; set; }

        [DataMember]
        public string ExpelledUsername { get; set; }

        [DataMember]
        public string Reason { get; set; }
    }
}
