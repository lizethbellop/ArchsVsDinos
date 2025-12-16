using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class GameEndedDTO
    {
        [DataMember]
        public string MatchCode { get; set; }

        [DataMember]
        public string Reason { get; set; }

        [DataMember]
        public int WinnerUserId { get; set; }

        [DataMember]
        public int WinnerPoints { get; set; }

        [DataMember]
        public List<PlayerScoreDTO> FinalScores { get; set; }
    }
}
