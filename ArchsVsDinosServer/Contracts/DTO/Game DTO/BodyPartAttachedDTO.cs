using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class BodyPartAttachedDTO
    {
        [DataMember]
        public string MatchCode { get; set; }

        [DataMember]
        public int PlayerUserId { get; set; }

        [DataMember]
        public int DinoInstanceId { get; set; }

        [DataMember]
        public CardDTO BodyCard { get; set; }

        [DataMember]
        public int NewTotalPower { get; set; }
    }
}
