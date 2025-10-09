using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    public class PlayerDTO
    {
        [DataMember]
        public int idPlayer { get; set; }
        [DataMember]
        public string facebook { get; set; }
        [DataMember]
        public string instagram { get; set; }
        [DataMember]
        public string x { get; set; }
        [DataMember]
        public int totalWins { get; set; }
        [DataMember]
        public int totalLosses { get; set; }
        [DataMember]
        public int totalPoints { get; set; }
    }
}
