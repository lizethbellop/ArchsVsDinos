using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Game_DTO.Swap
{
    [DataContract]
    public class SwapCardRequestDTO
    {
        [DataMember]
        public int CardToSwapId { get; set; } // ID de la carta que el jugador activo quiere ofrecer

        [DataMember]
        public int TargetPlayerId { get; set; } // ID del jugador al que se propone el intercambio
    }
}
