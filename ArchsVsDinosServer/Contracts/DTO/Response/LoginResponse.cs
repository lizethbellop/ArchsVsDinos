using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    [DataContract]
    public class LoginResponse
    {
        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public UserDTO UserSession { get; set; }

        [DataMember]
        public PlayerDTO AssociatedPlayer { get; set; }
    }
}
