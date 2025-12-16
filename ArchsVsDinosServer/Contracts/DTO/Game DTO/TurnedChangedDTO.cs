using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class TurnChangedDTO
    {
        [DataMember]
        public string MatchCode { get; set; }

        [DataMember]
        public int CurrentPlayerUserId { get; set; }

        [DataMember]
        public int TurnNumber { get; set; }

        [DataMember]
        public TimeSpan RemainingTime { get; set; }

        [DataMember]
        public Dictionary<int, int> PlayerScores { get; set; }
    }
}
