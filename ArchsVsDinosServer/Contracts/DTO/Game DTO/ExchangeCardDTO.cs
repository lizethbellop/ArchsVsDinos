using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class ExchangeCardDTO
    {
        [DataMember]
        public int CardIdToDiscard { get; set; }

        [DataMember]
        public int PileIndex { get; set; }
    }
}
