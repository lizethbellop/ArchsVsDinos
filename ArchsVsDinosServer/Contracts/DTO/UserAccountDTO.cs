using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO
{
    [DataContract]
    public class UserAccount
    {
        [DataMember]
        public int idUser { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string username { get; set; }
        public string nickname { get; set; }
        public int idConfiguration { get; set; }
        public int idPlayer { get; set; }

    }
}
