using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class CardExchangedDTO
    {
        [DataMember] public string MatchCode { get; set; }

        [DataMember] public int PlayerAUserId { get; set; }

        [DataMember] public int PlayerBUserId { get; set; }

        [DataMember] public CardDTO CardFromPlayerA { get; set; }

        [DataMember] public CardDTO CardFromPlayerB { get; set; }
    }

}
