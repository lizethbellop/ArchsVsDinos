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
        public string name { get; set; }
        [DataMember]
        public string email { get; set; }
        [DataMember]
        public string password { get; set; }
        [DataMember]
        public string username { get; set; }
        [DataMember]
        public string nickname { get; set; }
        [DataMember]
        public int idConfiguration { get; set; }
        [DataMember]
        public int idPlayer { get; set; }

    }
}
