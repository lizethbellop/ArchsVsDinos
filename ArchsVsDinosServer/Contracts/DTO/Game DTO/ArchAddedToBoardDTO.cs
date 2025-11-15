using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class ArchAddedToBoardDTO
    {
        [DataMember]
        public int MatchId { get; set; }

        [DataMember]
        public int PlayerUserId { get; set; }

        [DataMember]
        public string PlayerUsername { get; set; }

        [DataMember]
        public CardDTO ArchCard { get; set; }

        [DataMember]
        public string ArmyType { get; set; }

        [DataMember]
        public int NewArchCount { get; set; }
    }
}
