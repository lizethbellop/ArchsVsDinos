using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    [DataContract]
    public class UserAccountDTO
    {

        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Email { get; set; }
        [DataMember]
        public string Password { get; set; }
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public string Nickname { get; set; }
        [DataMember]
        public int IdConfiguration { get; set; }
        [DataMember]
        public int IdPlayer { get; set; }

    }
}
