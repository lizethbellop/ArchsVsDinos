using System.Runtime.Serialization;

namespace Contracts.DTO.Game_DTO
{
    [DataContract]
    public class CardTakenFromDiscardDTO
    {
        [DataMember]
        public string MatchCode { get; set; }

        [DataMember]
        public int PlayerUserId { get; set; }

        [DataMember]
        public int CardId { get; set; }

        [DataMember]
        public int RemainingCardsInDiscard { get; set; }
    }
}