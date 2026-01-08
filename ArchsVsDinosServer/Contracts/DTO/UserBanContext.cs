using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    [DataContract]
    public class UserBanContext
    {
        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public int Strikes { get; set; }

        [DataMember]
        public int Context { get; set; }

        [DataMember]
        public string MatchCode { get; set; }

        [DataMember]
        public bool IsGuest { get; set; }

        [DataMember]
        public int UserId { get; set; }
    }
}
